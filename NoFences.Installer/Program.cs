using System;
using System.Windows;
using WixSharp;
using WixSharp.CommonTasks;

class Script
{
    static public void Main(string[] args)
    {

        Compiler.SignAllFilesOptions.SignEmbeddedAssemblies = false;
        var project = new ManagedProject(
            "NoFences",
            new Dir(
                @"%ProgramFiles%\TinySoft\NoFences",
                new WixEntity[] {
                    new Files(@"..\NoFences\bin\Release\*.*")
                }
            )
        );
// TODO add .net core 4.8 as requirement project.SetNetFxPrerequisite("NetFx48Redist");
        project.SignAllFiles = true;

        if (Environment.GetEnvironmentVariable("CERTIFICATE_PATH") != null)
        {
            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH"),
                Password = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD"),
                TimeUrl = new UriBuilder("http://timestamp.digicert.com").Uri               
            };
        }
        else
        {
            project.DigitalSignature = new DigitalSignature
            {
                PfxFilePath = @"..\signing\NoFences_cert.pfx",
                Password = "NoFences",
                TimeUrl = new UriBuilder("http://timestamp.digicert.com").Uri,
                HashAlgorithm = HashAlgorithmType.sha256
            };
        }
        project.Load += msi_load;
        project.BeforeInstall += msi_before_install;
        project.AfterInstall += msi_after_install;
        project.UnhandledException += msi_unhandled_exception;

        project.GUID = new Guid("5CF1A403-6251-4CB6-A1EA-26A933614DDE");
        project.OutDir = "bin\\Release";

        //project.InstallPrivileges = InstallPrivileges.elevated;

        project.Actions = new WixSharp.Action[]
        {
            new WixQuietExecAction(new Id("InstallExtension"), "[INSTALLDIR]ServerRegistrationManager.exe", "install [INSTALLDIR]NoFencesExtensions.dll -codebase -os64", Return.check, When.After, Step.InstallFiles, WixSharp.Condition.NOT_Installed),
            new WixQuietExecAction(new Id("UninstallExtension"), "[INSTALLDIR]ServerRegistrationManager.exe", "uninstall [INSTALLDIR]NoFencesExtensions.dll", Return.check, When.Before, Step.RemoveFiles, WixSharp.Condition.Installed)
        };

        project.BuildMsi();
    }

    private static void msi_unhandled_exception(ExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "msi_unhandled_exception");
    }

    private static void msi_after_install(SetupEventArgs e)
    {
        MessageBox.Show(e.ToString(), "msi_after_install");
    }

    private static void msi_before_install(SetupEventArgs e)
    {
        MessageBox.Show(e.ToString(), "msi_before_install");
    }

    private static void msi_load(SetupEventArgs e)
    {
        MessageBox.Show(e.ToString(), "msi_load");
    }
}