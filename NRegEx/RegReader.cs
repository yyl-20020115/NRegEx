using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NRegEx;

public class RegReader
{
    public const int UNICODE_LIMIT = 0x10ffff;

    public const int TEXT_BEGINING = UNICODE_LIMIT + 1;
    public const int TEXT_ENDING = UNICODE_LIMIT + 2;


    public const int WORD_BOUNDARY = UNICODE_LIMIT + 1;

    public readonly TextReader Reader;
    public RegReader(TextReader reader)
    {
        this.Reader= reader;
    }
    public int Read()
    {
        return 0;
    }
}
