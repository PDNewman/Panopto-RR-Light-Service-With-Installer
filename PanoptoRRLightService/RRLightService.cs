﻿using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;

namespace RRLightProgram
{
    public partial class RRLightService : ServiceBase
    {
        /// <summary>
        /// We accept the self signed server by overriding in ServerCertificateValidationCallback.
        /// </summary>
        private const bool SelfSignedServer = true;
        
        private static bool serverCertificateValidationCallbackIsSet = false;

        private StateMachine stateMachine = null;

        private RemoteRecorderSync remoteRecorderSync = null;

        private DelcomLight delcomLight = null;

        private SerialComm serialComm = null;

        /// <summary>
        /// Constrocutor.
        /// </summary>
        public RRLightService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Manually run the application for testing.
        /// </summary>
        public void ManualRun()
        {
            this.OnStart(null);
        }

        /// <summary>
        /// Start handler.
        /// MSDN documentaion recommends not to rely on args in general.
        /// We use config file for any parameters and ignore command line args.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            EnsureCertificateValidation();

            ILightControl lightControl = null;

            this.stateMachine = new StateMachine();

            this.remoteRecorderSync = new RemoteRecorderSync((IStateMachine)this.stateMachine);

            if (string.Compare(Properties.Settings.Default.DeviceType, "Delcom", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // Set up of Delcom light (with button) device.
                this.delcomLight = new DelcomLight((IStateMachine)this.stateMachine);
                lightControl = this.delcomLight as ILightControl;

                if (!this.delcomLight.Start())
                {
                    Trace.TraceError("Failed to start up Delcom Light component. Terminate.");
                    throw new ApplicationException("Failed to start up Delcom Light component. Terminate.");
                }
            }
            else if (string.Compare(Properties.Settings.Default.DeviceType, "Serial", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // Set up serial port
                this.serialComm = new SerialComm((IStateMachine)this.stateMachine);
                lightControl = this.serialComm as ILightControl;

                if (!this.serialComm.Start())
                {
                    Trace.TraceError("Failed to start up Serial component. Terminate.");
                    throw new ApplicationException("Failed to start up Serial component. Terminate.");
                }
            }
            // TODO: add here for device specific start up when another device type is added.
            else
            {
                throw new InvalidOperationException("Specified device type is not supported: " + Properties.Settings.Default.DeviceType);
            }

             // Start processing of the state machine.
            this.stateMachine.Start(this.remoteRecorderSync, lightControl, console);
        }

        /// <summary>
        /// Stop handler.
        /// </summary>
        protected override void OnStop()
        {
            if (this.remoteRecorderSync != null)
            {
                this.remoteRecorderSync.Stop();
                this.remoteRecorderSync = null;
            }

            if (this.delcomLight != null)
            {
                this.delcomLight.Stop();
                this.delcomLight = null;
            }

            if (this.stateMachine != null)
            {
                this.stateMachine.Stop();
                this.stateMachine = null;
            }
        }

        /// <summary>
        /// Ensures that our custom certificate validation has been applied
        /// </summary>
        public static void EnsureCertificateValidation()
        {
            if (RRLightService.SelfSignedServer && !RRLightService.serverCertificateValidationCallbackIsSet)
            {
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(CustomCertificateValidation);
                serverCertificateValidationCallbackIsSet = true;
            }
        }

        /// <summary>
        /// Ensures that server certificate is authenticated
        /// </summary>
        private static bool CustomCertificateValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }
    }
}