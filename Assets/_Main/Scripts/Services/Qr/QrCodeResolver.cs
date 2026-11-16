using _Main.Scripts.Configs.Collection;
using _Main.Scripts.Services.Packs;
using JetBrains.Annotations;
using OneOf;

namespace _Main.Scripts.Services.Qr
{
	public enum QrRejectionReason
	{
		/// <summary>Not a game code at all (a URL, a product barcode payload, ...).</summary>
		NotAGameCode = 0,
		/// <summary>Right format, but no character with that id exists.</summary>
		UnknownCharacter = 1,
		/// <summary>The id exists but the secret does not match (typed/forged code).</summary>
		WrongSecret = 2,
		/// <summary>A game code for a collection this app doesn't have (yet): update the app.</summary>
		UnknownPack = 3
	}

	/// <summary>A scanned code that unlocks one character of one pack.</summary>
	public readonly struct ResolvedCode
	{
		public CollectionPack Pack { get; }
		public CharacterDefinition Character { get; }

		public ResolvedCode(CollectionPack pack, CharacterDefinition character)
		{
			Pack = pack;
			Character = character;
		}
	}

	public interface IQrCodeResolver
	{
		OneOf<ResolvedCode, QrRejectionReason> Resolve(string? rawPayload);
	}

	/// <summary>Maps a raw scanned string to the pack and character it unlocks (secrets checked against the shipped hashes).</summary>
	[UsedImplicitly]
	public sealed class QrCodeResolver : IQrCodeResolver
	{
		private readonly IPackLibrary _library;

		public QrCodeResolver(IPackLibrary library)
		{
			_library = library;
		}

		public OneOf<ResolvedCode, QrRejectionReason> Resolve(string? rawPayload)
		{
			if (!QrPayloadCodec.TryDecode(rawPayload, out var payload))
				return QrRejectionReason.NotAGameCode;

			CollectionPack pack;
			if (payload.PackId.Length > 0)
			{
				if (!_library.TryGetPack(payload.PackId, out pack))
					return QrRejectionReason.UnknownPack;
			}
			else if (!_library.TryGetPackByLegacyPrefix(payload.LegacyPrefix, out pack))
			{
				return QrRejectionReason.NotAGameCode;
			}

			if (!pack.TryGetCharacter(payload.CharacterId, out var character))
				return QrRejectionReason.UnknownCharacter;

			if (!PackCodes.Matches(pack.Id, character.Id, payload.Secret, character.CodeHash))
				return QrRejectionReason.WrongSecret;

			return new ResolvedCode(pack, character);
		}
	}
}
