using Eto.Forms;

namespace AM2RModPacker.Mac;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        new Application(Eto.Platforms.Mac64).Run(new ModPacker());
    }
}