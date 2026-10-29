# Porting pitfalls

Kotlin, Java and Android code whose closest C# equivalent behaves differently. Each entry gives upstream's behaviour and how to reproduce it in C#.

## Kotlin expressions and order of evaluation

| Upstream | Upstream behaviour | C# difference | Reproduce with |
|---|---|---|---|
| `a ?: if (c) { x } else { y } ?: b` | An `if` whose branches are braced blocks ends at the closing brace, so the last `?:` applies to the whole `if`: when `c` holds and `x` is null, `b` is still used. | `a ?? (c ? x : b)` uses `b` only when `c` is false. | `a ?? (c ? x : y) ?? b`. |
| `val x = expression` in a class body | Computed once, when the object is built. | A property with a getter body (`=>`) is computed on every read, so a value that depends on the current time can change between reads. | A field or an `init` property set when the object is built. |
| A check that reads state built up earlier in the same function, such as a list of errors | The check sees everything recorded before it in upstream's order. | Moving the check earlier, or copying the state into a local before it is complete, misses what is recorded after. | The same order of statements as upstream. |

Where Kotlin reads two ways, compiling a small Kotlin test settles it.

## Rounding

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `roundToInt()`, `roundToLong()`, Java `Math.round` | Halves round towards positive infinity: 2.5 gives 3, −2.5 gives −2. | `Math.Round` rounds halves to even by default, and `MidpointRounding.AwayFromZero` turns −2.5 into −3. | `Math.Floor(value + 0.5)`, as in `UnitRounding.RoundToLong`. |
| `kotlin.math.round()`, Java `Math.rint` | Halves round to even: 2.5 gives 2, 3.5 gives 4. | None, but the name looks like `roundToInt()` and behaves differently from it. | `Math.Round(value)`. |
| ICU `NumberFormatter`, `NumberFormat`, `DecimalFormat` | Halves round to even unless a rounding mode is set. | `Math.Round(value, decimals, MidpointRounding.AwayFromZero)` rounds them away from zero. | `Math.Round(value, decimals)`, or the rounding mode the upstream formatter sets. |

## Number formatting

Upstream formats numbers with `Number.format` in `weather-unit/.../formatting/NumberFormatting.kt`, on top of ICU.

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `Precision.maxFraction(n)`, `maximumFractionDigits = n` | At most `n` decimals, with no trailing zeros: 4.0 gives "4", 4.25 at one decimal gives "4.2". | `"F"` and `"N"` pad every decimal: 4.0 gives "4.0". | A custom format with optional digits, such as `"0.#"` for one decimal. |
| `Precision.integer()` after `roundToInt()` | A whole number rounded half towards positive infinity. | See Rounding. | Round with `Math.Floor(value + 0.5)` first, then format. |
| ICU grouping | Thousands are grouped following the locale's minimum grouping digits: "1,013" in English. | `"F"` and `"0.#"` never group, and `"N"` always groups. | A format with a group separator, such as `"#,##0.#"`, checked against the locale's minimum grouping digits. |
| `SignDisplay.ALWAYS` | A plus sign before positive values. | Standard formats show no plus sign. | A section format such as `"+0;-0;0"`. |

## Integer arithmetic

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `a.mod(b)`, `Math.floorMod` | The result has the sign of `b`: −1 mod 24 gives 23. | `%` gives the sign of `a`: −1 % 24 gives −1. | `((a % b) + b) % b`. |
| `Math.floorDiv` | Rounds the quotient down: −7 / 2 gives −4. | `/` truncates towards zero: −7 / 2 gives −3. | `Math.Floor((double)a / b)`, or a check on the remainder. |

Kotlin's `/` and `%` behave like C#'s.

## Floating point

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `Float` values and literals such as `60f` | 32-bit precision. | `double` keeps more digits, so a comparison or a stored value can differ. | `float` wherever the result is compared, stored or displayed at full precision. |
| `==` on `Double?` or other values not typed as `Double` | Uses `equals`: `NaN == NaN` is true and `0.0 == -0.0` is false. | `==` on `double?` follows IEEE 754: `NaN == NaN` is false. | `.Equals` where upstream compares nullable or boxed values. |

## Collections

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `sorted`, `sortedBy`, `sortedWith`, `sortBy` | Stable: equal elements keep their order. | `List.Sort` and `Array.Sort` are unstable. | LINQ `OrderBy` and `ThenBy`, which are stable. |
| `average()` on an empty collection | Returns `NaN`. | LINQ `Average` throws on an empty sequence of non-nullable numbers. | A check for an empty sequence. |
| `maxOrNull()`, `minOrNull()` | Return `null` on an empty collection. | LINQ `Max` and `Min` throw on an empty sequence of non-nullable values. | `MaxBy`, a nullable sequence, or a check for an empty sequence. |
| `a until b`, `a..<b` and `a..b` | `until` and `..<` exclude `b`, while `..` includes it. | A `for` loop states its bound explicitly. | `i < b` for `until`, `i <= b` for `..`. |

## Text and culture

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `lowercase()`, `uppercase()` | Culture-independent. | `ToLower()` and `ToUpper()` use the current culture. | `ToLowerInvariant()` and `ToUpperInvariant()`. |
| `compareTo` and `sorted()` on strings | Ordinal comparison of UTF-16 code units. | `string.Compare`, `CompareTo` and `OrderBy` on strings compare by culture. | `StringComparer.Ordinal` and `string.CompareOrdinal`. |
| `Double.toString()`, `Int.toString()` | Culture-independent. | `ToString()` uses the current culture. | `ToString(CultureInfo.InvariantCulture)` for storage and identifiers, `Strings.Culture` for display. |
| `String.format` | Uses the default locale. | `string.Format` uses the current culture. | An explicit culture. |

## Dates and time zones

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `Calendar` and `Date` with a location's `TimeZone` | Days, hours and day boundaries fall in the location's time zone. | `DateTime.Now`, `ToLocalTime` and `DateTimeKind.Local` use the computer's time zone. | `TimeZoneInfo.ConvertTime` with the location's IANA time zone. |

## Platform

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| Android's network check before a refresh | Without a connection, the refresh stops with a network error and the saved weather is left untouched. | `NetworkInterface.GetIsNetworkAvailable()` is true whenever any adapter is up, including VPN and Hyper-V virtual adapters, so most offline cases pass. | Windows' connectivity level for the internet connection profile. |

## Android resources

| Upstream | Upstream behaviour | .NET difference | Reproduce with |
|---|---|---|---|
| `getQuantityString` | Picks the plural form with the CLDR rules of the language. | `.resx` has no plurals. | `PluralRules`. |
| String resources | A string missing from a language falls back to the default language. | `ResourceManager` falls back through parent cultures. | `Strings`, which falls back to English. |
