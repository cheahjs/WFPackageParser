WFPackageParser
===============

Library to parse Warframe's Packages.bin and to parse localization strings from Languages.bin.

Notes for package parsing
-------------------------

* All values will be converted into strings, regardless of original type (strings, floats, ints).
* Most arrays are handled properly.
* There may be edge cases where the library fails to parse.
* Packages may contain duplicate keys, the library will append numbers to the end of the key to prevent collisions.
* Tested with U13.5.3 without any errors.

Final output should contain the following types:
* Strings
* Objects
* Arrays

Notes for language parsing
--------------------------

If parsing a language other than en, you need to fallback to the en list when lookup fails in the language dictionary.
