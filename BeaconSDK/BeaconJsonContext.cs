using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.Accounts;
using Tavstal.BeaconSDK.Models.Java;
using Tavstal.BeaconSDK.Models.MojangApi.User;

namespace Tavstal.BeaconSDK;

/// <summary>
///     Source-generated <see cref="JsonSerializerContext" /> containing the serialization metadata
///     for the SDK's accounts, account skins, Mojang profiles, skins, capes and Java mirror configuration.
/// </summary>
[JsonSerializable(typeof(Account))]
[JsonSerializable(typeof(List<Account>))]
[JsonSerializable(typeof(AccountSkin))]
[JsonSerializable(typeof(List<AccountSkin>))]
[JsonSerializable(typeof(MojangProfile))]
[JsonSerializable(typeof(Skin))]
[JsonSerializable(typeof(List<Skin>))]
[JsonSerializable(typeof(Cape))]
[JsonSerializable(typeof(List<Cape>))]
[JsonSerializable(typeof(JavaMirrorConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true, IgnoreReadOnlyFields = true,
    Converters = [typeof(JsonStringEnumConverter<EAccountType>)])]
public partial class BeaconJsonContext : JsonSerializerContext;
