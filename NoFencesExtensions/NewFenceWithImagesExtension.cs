using NoFences.Model;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace NoFences
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class NewFenceWithImagesExtension : SharpContextMenu
    {

        public NewFenceWithImagesExtension() {
            Log("Initializing extension");
            Log("REgistering pipeClient");
            Log("pipeClient connected");
        }

        protected override bool CanShowMenu()
        {
            return true;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            //  Create the menu strip
            var menu = new ContextMenuStrip();

            //  Create a 'count lines' item
            var itemCountLines = new ToolStripMenuItem
            {
                Text = "New Fence from here...",
                //Image = Properties.Resources.CountLines
            };

            //  When we click, we'll count the lines
            itemCountLines.Click += (sender, args) => CreateNewFence();
            

            //  Add the item to the context menu.
            menu.Items.Add(itemCountLines);

            //  Return the menu
            return menu;
        }

        private void CreateNewFence()
        {

            Log("Creating new Fence");
            var fenceInfo = new FenceInfo(Guid.NewGuid())
            {
                Name = "New Fence",
                PosX = 100,
                PosY = 250,
                Height = 300,
                Width = 300
            };

            if (SelectedItemPaths.Count() >= 1)
            {
                if (SelectedItemPaths.Count((path) => path.EndsWith("jpg")) == SelectedItemPaths.Count())
                {
                    if (SelectedItemPaths.Count((path) => path.EndsWith("jpg")) > 1) { 
                        fenceInfo.Type = EntryType.SlideShow.ToString();
                        fenceInfo.Folder = Directory.EnumerateDirectories(SelectedItemPaths.First()).First();
                    } else
                    {
                        fenceInfo.Type = EntryType.Picture.ToString();
                        fenceInfo.Folder = SelectedItemPaths.First();
                    }
                }
                else
                {
                    fenceInfo.Files = SelectedItemPaths.ToList();
                }
            }

            Log("Generating event to send to the main application");
            SendCreateFenceMessage(fenceInfo);
            
        }

        private void SendCreateFenceMessage(FenceInfo fenceInfo)
        {

            var serializer = new XmlSerializer(typeof(FenceInfo));
            using (var textWriter = new StringWriter()) { 

                serializer.Serialize(textWriter, fenceInfo);

                var fenceInfostr = textWriter.GetStringBuilder().ToString();
                Log("Content: \n" + fenceInfostr);
                try
                {
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "NoFencesPipeServer", PipeDirection.Out))
                    {
                        Log($"PipeClient connected ? {pipeClient.IsConnected}");
                        if (!pipeClient.IsConnected)
                        {
                            Log("Connecting");
                            pipeClient.Connect(200);
                        }

                        Log($"Number of servers available {pipeClient.NumberOfServerInstances}");

                        using (StreamWriter stream = new StreamWriter(pipeClient))
                        {
                            stream.AutoFlush = true;
                            Log("Writing message");
                            stream.WriteLine(fenceInfostr);
                            Log("Waiting for pipe draining");
                            pipeClient.WaitForPipeDrain();

                        }
                        Log("Successfully sent event, disposing client");
                        pipeClient.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    LogError("Error sending even to the pipe client", ex);
                }
            }
        }
    }
}
