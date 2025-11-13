using System;
using System.Diagnostics;
using System.IO;
using WindowsInstaller;
using WixSharp;
using WixSharp.Bootstrapper;
using WixSharp.CommonTasks;
using WixToolset.Dtf.WindowsInstaller;
using Condition = WixSharp.Condition;
using File = System.IO.File;

public class CustomActions
{
    [CustomAction]
    public static ActionResult RegisterExtension(Session session)
    {
        session.Log("Begin RegisterExtension");
        return session.HandleErrors(() =>
        {
            try
            {
                string installDir = session.Property("INSTALLDIR");
                string exePath = Path.Combine(installDir, "ServerRegistrationManager.exe");
                string dllPath = Path.Combine(installDir, "NoFencesExtensions.dll");
                if (!File.Exists(exePath))
                {
                    session.Log("ServerRegistrationManager.exe not found at " + exePath);
                    throw new Exception("ServerRegistrationManager.exe not found.");
                }
                Process process = new Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = $"install \"{dllPath}\" -codebase -os64";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                session.Log($"Running command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                session.Log("Output: " + output);
                session.Log("Error: " + error);
                if (process.ExitCode != 0)
                {
                    session.Log("Error registering extension. Exit code: " + process.ExitCode);
                    throw new Exception("Error registering extension.");
                }
                session.Log("Extension registered successfully.");
            }
            catch (Exception ex)
            {
                session.Log("Exception in RegisterExtension: " + ex.ToString());
                throw ex;
            }
        });
    }

    [CustomAction]
    public static ActionResult UnregisterExtension(Session session)
    {
        session.Log("Begin UnregisterExtension");
        return session.HandleErrors(() =>
        {
            try
            {
                string installDir = session.Property("INSTALLDIR");
                string exePath = Path.Combine(installDir, "ServerRegistrationManager.exe");
                string dllPath = Path.Combine(installDir, "NoFencesExtensions.dll");

                if (!File.Exists(exePath))
                {
                    session.Log("ServerRegistrationManager.exe not found at " + exePath); // Ignore if not found
                    return;
                }

                Process process = new Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = $"uninstall \"{dllPath}\" ";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                session.Log($"Running command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                session.Log("Output: " + output);
                session.Log("Error: " + error);

                if (process.ExitCode != 0)
                {
                    session.Log("Error unregistering extension. Exit code: " + process.ExitCode);
                    // Do not fail the uninstall if this fails
                }
                else
                {
                    session.Log("Extension unregistered successfully.");
                }
            }
            catch (Exception ex)
            {
                session.Log("Exception in UnregisterExtension: " + ex.ToString());
                throw ex;
            }
        });
    }

    [CustomAction]
    public static ActionResult RegisterService(Session session)
    {
        return session.HandleErrors(() =>
        {
            string serviceExePath = session.Property("INSTALLDIR") + "NoFences.Service.exe";
            string serviceName = "NoFencesService";

            session.Log($"RegisterService: Begin service installation for {serviceName}");
            session.Log($"Service executable path: {serviceExePath}");

            try
            {
                // Step 1: Check if service already exists
                bool serviceExists = Tasks.IsServiceInstalled(serviceName);
                session.Log($"Service exists check: {serviceExists}");

                if (serviceExists)
                {
                    session.Log($"Service {serviceName} already exists - performing cleanup before reinstall");

                    // Step 2: Stop service if it's running
                    try
                    {
                        session.Log($"Attempting to stop service {serviceName}");
                        Tasks.StopService(serviceName, throwOnError: false);
                        session.Log($"Service {serviceName} stopped successfully (or was not running)");
                    }
                    catch (Exception ex)
                    {
                        session.Log($"Warning: Could not stop service: {ex.Message}");
                        // Continue anyway - service might not be running
                    }

                    // Step 3: Uninstall old service
                    try
                    {
                        session.Log($"Attempting to uninstall existing service {serviceName}");
                        Tasks.InstallService(serviceExePath, install: false);
                        session.Log($"Service {serviceName} uninstalled successfully");
                    }
                    catch (Exception ex)
                    {
                        session.Log($"Warning: Could not uninstall existing service: {ex.Message}");
                        // Log but continue - we'll try to install anyway
                    }
                }

                // Step 4: Install service
                session.Log($"Installing service {serviceName}");
                Tasks.InstallService(serviceExePath, install: true);
                session.Log($"Service {serviceName} installed successfully");

                // Step 5: Start service
                session.Log($"Starting service {serviceName}");
                Tasks.StartService(serviceName, throwOnError: false);
                session.Log($"Service {serviceName} started successfully");
            }
            catch (Exception ex)
            {
                session.Log($"ERROR in RegisterService: {ex.Message}");
                session.Log($"Stack trace: {ex.StackTrace}");
                throw;
            }
        });
    }

    [CustomAction]
    public static ActionResult UnregisterService(Session session)
    {
        return session.HandleErrors(() =>
        {
            string serviceExePath = session.Property("INSTALLDIR") + "NoFences.Service.exe";
            string serviceName = "NoFencesService";

            session.Log($"UnregisterService: Begin service uninstallation for {serviceName}");

            try
            {
                // Step 1: Check if service exists
                bool serviceExists = Tasks.IsServiceInstalled(serviceName);
                session.Log($"Service exists check: {serviceExists}");

                if (!serviceExists)
                {
                    session.Log($"Service {serviceName} does not exist - nothing to uninstall");
                    return; // Nothing to do
                }

                // Step 2: Stop service if running
                try
                {
                    session.Log($"Attempting to stop service {serviceName}");
                    Tasks.StopService(serviceName, throwOnError: false);
                    session.Log($"Service {serviceName} stopped successfully");
                }
                catch (Exception ex)
                {
                    session.Log($"Warning: Could not stop service: {ex.Message}");
                    // Continue anyway - we still want to uninstall
                }

                // Step 3: Uninstall service
                session.Log($"Uninstalling service {serviceName}");
                Tasks.InstallService(serviceExePath, install: false);
                session.Log($"Service {serviceName} uninstalled successfully");
            }
            catch (Exception ex)
            {
                session.Log($"ERROR in UnregisterService: {ex.Message}");
                session.Log($"Stack trace: {ex.StackTrace}");
                // Don't throw - we don't want uninstall to fail if service removal fails
                session.Log("Uninstall will continue despite service unregistration error");
            }
        });
    }
}

class Script
{
    static public void Main(string[] args)
    {
        Compiler.SignAllFilesOptions.SignEmbeddedAssemblies = false;
        Compiler.SignAllFilesOptions.SkipSignedFiles = true;

        var project = new ManagedProject(
            "NoFences",
            new Dir(
                @"%ProgramFiles%\TinySoft\NoFences",
                new Files(@"..\NoFences\bin\Release\*.*"))
            ,
            new Dir(@"%ProgramMenu%\TinySoft",
                new ExeFileShortcut("NoFences", "[INSTALLDIR]NoFences.exe", ""),
                new ExeFileShortcut("Uninstall NoFences", "[System64Folder]msiexec.exe", "/x [ProductCode]")
            )
        );

        project.SignAllFiles = true;

        // Session 12 Continuation: Use environment variables for CI/CD certificate
        if (Environment.GetEnvironmentVariable("CERTIFICATE_PATH") != null)
        {
            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH"),
                Password = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD"),
                TimeUrl = new UriBuilder("http://timestamp.digicert.com").Uri
            };
        }
        // Session 12 Continuation: Only use local signing folder if it exists
        else if (System.IO.File.Exists(@"..\signing\NoFences_cert.pfx"))
        {
            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = @"..\signing\NoFences_cert.pfx",
                Password = "NoFences",
                TimeUrl = new UriBuilder("http://timestamp.digicert.com").Uri,
                HashAlgorithm = HashAlgorithmType.sha256
            };
        }
        // Session 12 Continuation: If no certificate is available, skip signing (e.g., in CI without secrets)
        else
        {
            Console.WriteLine("WARNING: No certificate found. MSI will not be signed.");
            Console.WriteLine("  - For CI/CD: Set CERTIFICATE_PATH and CERTIFICATE_PASSWORD environment variables");
            Console.WriteLine("  - For local dev: Place certificate in ../signing/NoFences_cert.pfx");
        }

        project.GUID = new Guid("5CF1A403-6251-4CB6-A1EA-26A933614DDE");
        project.OutDir = "bin\\Release";
        project.LicenceFile = @"LICENSE.rtf";

        //project.Actions = new WixSharp.Action[]
        //{
        //    new WixQuietExecAction(new Id("InstallExtension"), "cmd.exe", "/c \"'[INSTALLDIR]ServerRegistrationManager.exe'\" install \"[INSTALLDIR]NoFencesExtensions.dll\" -codebase -os64\"", Return.check, When.Before, Step.InstallFinalize, WixSharp.Condition.NOT_Installed),
        //    new WixQuietExecAction(new Id("UninstallExtension"), "cmd.exe", "/c \"'[INSTALLDIR]ServerRegistrationManager.exe'\" uninstall \"[INSTALLDIR]NoFencesExtensions.dll\"\"", Return.check, When.Before, Step.RemoveFiles, WixSharp.Condition.Installed)
        //};

        project.Actions = new WixSharp.Action[]
{
            new ElevatedManagedAction(CustomActions.RegisterService, Return.check, When.After, Step.InstallFiles, Condition.NOT_Installed),
            new ElevatedManagedAction(CustomActions.RegisterExtension, Return.check, When.After, Step.InstallFiles, Condition.NOT_Installed),
            new ElevatedManagedAction(CustomActions.UnregisterExtension, Return.check, When.Before, Step.RemoveFiles, Condition.Installed)
        };

        string msiFile = project.BuildMsi();

        var bundle = new Bundle("NoFences",
                          new PackageGroupRef("NetFx48Web"),
                          new MsiPackage(msiFile)
                          {
                              DisplayInternalUI = true
                          }
                      );

        bundle.Include(WixExtension.NetFx);

        bundle.Version = project.Version;
        bundle.UpgradeCode = new Guid("5CF1A403-6251-4CB6-A1EA-26A933614DDE");
        bundle.Application.LogoFile = @"..\NoFences\fibonacci.ico";
        bundle.Application.LicensePath = @"LICENSE.rtf";
        bundle.OutDir = "bin\\Release";

        bundle.Build("bin\\Release\\NoFences.bootstrap.exe");
    }
}