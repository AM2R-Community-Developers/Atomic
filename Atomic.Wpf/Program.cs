using Eto.Forms;

namespace Atomic.Wpf;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        new Application(Eto.Platforms.Wpf).Run(new ModPacker());
    }
}