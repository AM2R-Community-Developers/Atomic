using Eto.Forms;

namespace AM2RModPacker.Wpf;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        new Application(Eto.Platforms.Wpf).Run(new ModPacker());
    }
}