using Eto.Forms;

namespace Atomic.Mac;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var application = new Application(Eto.Platforms.Mac64);
        application.UnhandledException += ApplicationOnUnhandledException;
        try
        {
            application.Run(new ModPacker());
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unhandled Exception!\n*****Stack Trace*****\n\n{e}");
        }
        
    }
    private static void ApplicationOnUnhandledException(object sender, Eto.UnhandledExceptionEventArgs e)
    {
        Application.Instance.Invoke(() =>
        {
            MessageBox.Show($"Unhandled Exception!\n*****Stack Trace*****\n\n{e.ExceptionObject}", "GTK", MessageBoxType.Error);
        });
    }
}