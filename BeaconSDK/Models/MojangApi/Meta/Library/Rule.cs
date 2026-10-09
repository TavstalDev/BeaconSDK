using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;

/// <summary>
///     Represents a rule with an action and an optional operating system specification.
/// </summary>
public class Rule
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Rule" /> class.
    /// </summary>
    public Rule()
    {
        Action = string.Empty;
        Os = new RuleOs();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Rule" /> class with specified action and operating system.
    /// </summary>
    /// <param name="action">The action of the rule.</param>
    /// <param name="oS">The operating system specification for the rule.</param>
    public Rule(string action, RuleOs oS)
    {
        Action = action;
        Os = oS;
    }

    /// <summary>
    ///     Gets or sets the action of the rule (e.g., allow or disallow).
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; }

    /// <summary>
    ///     Gets or sets the operating system specification for the rule, if applicable.
    /// </summary>
    [JsonPropertyName("os")]
    public RuleOs Os { get; set; }
}
