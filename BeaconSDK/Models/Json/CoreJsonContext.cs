using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Models.Microsoft;
using Tavstal.BeaconSDK.Models.ModLoaders.Fabric;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Legacy;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Modern;
using Tavstal.BeaconSDK.Models.ModLoaders.NeoForge;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;
using Tavstal.BeaconSDK.Models.MojangApi.Requests;
using Tavstal.BeaconSDK.Models.MojangApi.User;
using ForgeVersionMetaModern = Tavstal.BeaconSDK.Models.ModLoaders.Forge.Modern.ForgeVersionMeta;
using ForgeVersionMetaLegacy = Tavstal.BeaconSDK.Models.ModLoaders.Forge.Legacy.ForgeVersionMeta;

namespace Tavstal.BeaconSDK.Models.Json;

/// <summary>
///     Represents a custom JSON serializer context for serializing various game-related objects.
/// </summary>
[JsonSerializable(typeof(VersionManifest))]
[JsonSerializable(typeof(FabricVersionMeta))]
[JsonSerializable(typeof(ForgeProfile))]
[JsonSerializable(typeof(ForgeVersionMetaLegacy), TypeInfoPropertyName = "ForgeVersionMetaLegacy")]
[JsonSerializable(typeof(ForgeVersionMetaModern), TypeInfoPropertyName = "ForgeVersionMetaModern")]
[JsonSerializable(typeof(ForgeVersionProfile))]
[JsonSerializable(typeof(List<ForgeManifest>))]
[JsonSerializable(typeof(List<NeoForgeManifest>))]
[JsonSerializable(typeof(List<Rule>))]
[JsonSerializable(typeof(DeviceCodeResult))]
[JsonSerializable(typeof(XboxTokenRequestBody))]
[JsonSerializable(typeof(XboxXstsRequestBody))]
[JsonSerializable(typeof(MinecraftAccessRequestBody))]
[JsonSerializable(typeof(OwnershipData))]
[JsonSerializable(typeof(MojangProfile))]
[JsonSerializable(typeof(VersionMeta))]
[JsonSerializable(typeof(ShowCapeRequestBody))]
[JsonSerializable(typeof(ChangeSkinRequestBody))]
[JsonSourceGenerationOptions(WriteIndented = true, IgnoreReadOnlyProperties = true, IgnoreReadOnlyFields = true)]
public partial class CoreJsonContext : JsonSerializerContext
{
}
