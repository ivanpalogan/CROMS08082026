using System;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace CROMS.Data
{
    /// <summary>
    /// Optional QR generation for the mobile-connection URL. Uses the QRCoder
    /// library IF it is present (bin\QRCoder.dll — install once with NuGet:
    /// "Install-Package QRCoder"), loaded via reflection so the project builds
    /// and runs with NO hard dependency. When QRCoder is absent, TryCreate
    /// returns null and the Login screen simply shows the URL text instead.
    /// </summary>
    public static class QrHelper
    {
        public static Bitmap TryCreate(string text, int pixelsPerModule = 6)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                Assembly asm = Assembly.Load("QRCoder");
                Type genType = asm.GetType("QRCoder.QRCodeGenerator");
                Type eccType = asm.GetType("QRCoder.QRCodeGenerator+ECCLevel");
                Type qrType = asm.GetType("QRCoder.QRCode");
                if (genType == null || eccType == null || qrType == null) return null;

                object gen = Activator.CreateInstance(genType);
                object ecc = Enum.Parse(eccType, "Q");

                // CreateQrCode has optional parameters, so match by the first two
                // param types (string, ECCLevel) and fill the rest with their
                // defaults — a fixed 2-arg GetMethod would not find the overload.
                MethodInfo create = genType.GetMethods()
                    .Where(mi => mi.Name == "CreateQrCode")
                    .FirstOrDefault(mi =>
                    {
                        var ps = mi.GetParameters();
                        return ps.Length >= 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == eccType;
                    });
                if (create == null) return null;

                ParameterInfo[] pars = create.GetParameters();
                object[] args = new object[pars.Length];
                args[0] = text;
                args[1] = ecc;
                for (int i = 2; i < pars.Length; i++)
                    args[i] = pars[i].HasDefaultValue ? pars[i].DefaultValue
                             : (pars[i].ParameterType.IsValueType ? Activator.CreateInstance(pars[i].ParameterType) : null);

                object data = create.Invoke(gen, args);

                object qr = Activator.CreateInstance(qrType, data);
                MethodInfo getGraphic = qrType.GetMethod("GetGraphic", new[] { typeof(int) });
                if (getGraphic == null) return null;
                return getGraphic.Invoke(qr, new object[] { pixelsPerModule }) as Bitmap;
            }
            catch
            {
                return null; // QRCoder not installed / any failure → no QR, URL still shown
            }
        }
    }
}
