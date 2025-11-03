using NoFences.Model;
using NoFences.View;
using NoFences.View.Fences.Handlers;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace NoFences.ApplicationLogic
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
            new FenceWindow(fenceInfo, CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceHandlerFactory>()).Show();
        }
    }
}
