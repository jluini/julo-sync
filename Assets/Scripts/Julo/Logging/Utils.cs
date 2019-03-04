using System.Text;

namespace Julo.Logging
{

    public class Utils
    {
        public static string Escaped(string original)
        {
            var sb = new StringBuilder();

            AppendEscaped(sb, original);

            return sb.ToString();
        }

        public static void AppendEscaped(StringBuilder builder, string original)
        {
            for(int i = 0; i < original.Length; i++)
            {
                if(original[i] == '<')
                    //builder.Append('└');
                    builder.Append(" < ");
                else if(original[i] == '>')
                    //builder.Append('┐');
                    builder.Append(" > ");
                else
                    builder.Append(original[i]);
            }
        }

    }

} // namespace Julo.Logging
