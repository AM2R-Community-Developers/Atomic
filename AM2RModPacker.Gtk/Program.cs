using Eto.Forms;

namespace AM2RModPacker.Gtk;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        new Application(Eto.Platforms.Gtk).Run(new ModPacker());
    }
}