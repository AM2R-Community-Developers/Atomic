using Eto.Forms;

namespace Atomic.Gtk;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        new Application(Eto.Platforms.Gtk).Run(new ModPacker());
    }
}