using System.Linq;
using System.Text;

namespace Amilious.Core.Extensions {
    
    public static class ObjectExtension {
        
        public static string GetFormattedClassName(this object obj) {
            var className = obj.GetType().ToString();
            className = className.Split('.').Last();
            var builder = new StringBuilder();
            for(var i = 0; i < className.Length; i++) {
                var c = className[i];
                if(i==0) c = char.ToUpper(c);
                if(char.IsUpper(c) && i != 0) builder.Append(" ");
                builder.Append(c);
            }
            return builder.ToString();
        }
        
    }
}