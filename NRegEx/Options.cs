namespace NRegEx;

// Parser flags.
[Flags]
public enum Options : uint
{
    None = 0,
    // Fold case during matching (case-insensitive).
    FOLD_CASE = 0x01,
    // Treat pattern as a literal string instead of a regexp.
    LITERAL = 0x02,
    // Allow character classes like [^a-z] and [[:space:]] to match newline.
    CLASS_NL = 0x04,
    // Allow '.' to match newline.
    DOT_NL = 0x08,
    // Treat ^ and $ as only matching at beginning and end of text, not
    // around embedded newlines.  (Perl's default).
    ONE_LINE = 0x10,
    // Make repetition operators default to non-greedy.
    NON_GREEDY = 0x20,
    // allow Perl extensions:
    //   non-capturing parens - (?: )
    //   non-greedy operators - *? +? ?? {}?
    //   flag edits - (?i) (?-i) (?i: )
    //     i - FoldCase
    //     m - !OneLine
    //     s - DotNL
    //     U - NonGreedy
    //   line ends: \A \z
    //   \Q and \E to disable/enable metacharacters
    //   (?P<name>expr) for named captures
    // \C (any byte) is not supported.
    PERL_X = 0x40,
    // Allow \p{Han}, \P{Han} for Unicode group and negation.
    UNICODE_GROUPS = 0x80,
    // Regexp END_TEXT was $, not \z.  Internal use only.
    WAS_DOLLAR = 0x100,
    MATCH_NL = CLASS_NL | DOT_NL,
    // As close to Perl as possible.
    PERL = CLASS_NL | ONE_LINE | PERL_X | UNICODE_GROUPS,
    // POSIX syntax.
    POSIX = 0,
}
