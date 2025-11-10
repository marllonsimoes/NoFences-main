using CommunityToolkit.Mvvm.DependencyInjection;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Model.Canvas;
using System;
using System.IO;
using System.IO.Pipes;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace NoFences.Services
{
    internal class PipeService : IApplicationService
    {
        private NamedPipeServerStream pipeServer;
        private Timer timer;

        public void Start()
        {
            StartPipeAndTimer();
        }

        public void Stop()
        {
            if (pipeServer != null)
            {
                if (pipeServer.IsConnected)
                {
                    pipeServer.Disconnect();
                }
                pipeServer.Dispose();
                pipeServer = null;
            }

            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        private void StartPipeAndTimer()
        {
            Stop(); // Ensure everything is clean before starting

            pipeServer = new NamedPipeServerStream("NoFencesPipeServer", PipeDirection.In);
            pipeServer.WaitForConnectionAsync();

            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (pipeServer.IsConnected)
                {
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        var result = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(result))
                        {
                            var serializer = new XmlSerializer(typeof(FenceInfo));
                            using (var stringReader = new StringReader(result))
                            {
                                var fence = (FenceInfo)serializer.Deserialize(stringReader);
                                if (Application.OpenForms.Count > 0)
                                {
                                    Application.OpenForms[0].Invoke(new Action(() => ShowFenceWindow(fence)));
                                }
                            }
                        }
                    }
                    // Re-initialize for the next connection
                    StartPipeAndTimer();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading pipe: {0}", ex);
            }
        }

        private void ShowFenceWindow(FenceInfo fenceInfo)
        {
            // Use the FenceManager to create the fence in the canvas-based system
            var fenceManager = Ioc.Default.GetService<FenceManager>();
            fenceManager?.CreateFence(fenceInfo.Name, fenceInfo);
        }
    }
}
