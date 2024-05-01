using System;

namespace EAVFW.Extensions.Infrastructure.Crypto
{
    public static class EntityFrameworkHelper
    {
        public static int Compare(this byte[] b1, byte[] b2)
        {
            throw new Exception("This method can only be used in EF LINQ Context");
        }
    }
}
