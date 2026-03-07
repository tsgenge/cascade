using System.Security.Cryptography;
using System.Text;

namespace CascadeEsdm.SharedKernel.Extensions;

public static class StringExtensions
{
    private static readonly MD5 Md5 = MD5.Create();

    public static Guid ToGuid(this string value)
    {
        if (!string.IsNullOrWhiteSpace(value)) {
            return new Guid(Md5.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
        else {
            return Guid.Empty;
        }
    }
}