using System.Text.Json;
using System.Text.Json.Serialization;
using StrongTypes;

namespace ProductReviews.Api.Features.Reviews;

/// <summary>A whole-star product rating, 1–5. Declared with the same three-line
/// recipe the library's own numeric wrappers use: <c>[NumericWrapper]</c> plus a
/// <c>Value</c> property and a <c>TryCreate</c>; the source generator emits the
/// rest (Create, Parse/TryParse, IParsable, comparison and equality operators).</summary>
[NumericWrapper(InvariantDescription = "a whole-star rating between 1 and 5")]
[JsonConverter(typeof(RatingJsonConverter))]
public readonly partial struct Rating
{
    public const int MinimumStars = 1;
    public const int MaximumStars = 5;

    // Stored as (Value - MinimumStars) so default(Rating) is a valid one-star rating.
    private readonly int offsetFromMinimum;

    private Rating(int offsetFromMinimum) => this.offsetFromMinimum = offsetFromMinimum;

    public int Value => offsetFromMinimum + MinimumStars;

    public static Rating? TryCreate(int value)
        => value is >= MinimumStars and <= MaximumStars ? new Rating(value - MinimumStars) : null;

    // The generator emits Equals(int?)/CompareTo(int?), which for a concrete
    // (non-generic) wrapper does not satisfy IEquatable<int>/IComparable<int> —
    // these two exact-signature members close that gap.
    public bool Equals(int other) => Value.Equals(other);

    public int CompareTo(int other) => Value.CompareTo(other);
}

/// <summary>Wire format is the bare integer, exactly like the library's numeric
/// wrappers; out-of-range JSON fails deserialization before any action runs.</summary>
public sealed class RatingJsonConverter : JsonConverter<Rating>
{
    public override Rating Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
        {
            throw new JsonException($"The JSON value could not be converted to {nameof(Rating)}.");
        }

        return Rating.TryCreate(value)
            ?? throw new JsonException($"The JSON value '{value}' cannot be converted to {nameof(Rating)}.");
    }

    public override void Write(Utf8JsonWriter writer, Rating value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}
