using _Main.Scripts._ProjectAgnostic.Ecs.BetterArchApi;
using _Main.Scripts._ProjectAgnostic.Ecs.UnityIntegration.Systems;
using _Main.Scripts._ProjectAgnostic.Utils;
using _Main.Scripts.Ecs.Collection.Cmp;
using _Main.Scripts.Ecs.GameSaving.Cmp;
using _Main.Scripts.Ecs.Scoring.Cmp;
using _Main.Scripts.Services.Packs;
using Arch.Core;

namespace _Main.Scripts.Ecs.Collection.Sys
{
	/// <summary>
	/// Consumes <see cref="ResetCollectionReq"/>: forgets one collection's progress and takes its points back off the
	/// total (through <see cref="UpdateScoreReq"/>, so it must run before <c>UpdateScoreSys</c>). Other collections and
	/// the session stay untouched.
	/// </summary>
	public sealed class ResetCollectionSys : IEcsTickSystem
	{
		private readonly World _world;
		private readonly IPackLibrary _library;
		private readonly Query _resetReqQuery;

		public ResetCollectionSys(World world, IPackLibrary library)
		{
			_world = world;
			_library = library;
			_resetReqQuery = _world.Query(new QueryDescription().WithAll<ResetCollectionReq>());
		}

		public void OnTick()
		{
			if (!_resetReqQuery.TryGetFirstEntity(out var entity))
				return;

			string packId = _world.Get<ResetCollectionReq>(entity).PackId;
			_world.DestroyAllEntitiesInQuery(_resetReqQuery);

			ref var collection = ref _world.GetSinglCmp<CollectionSinglCmp>();
			if (!collection.Packs.TryGetValue(packId, out var progress))
				return;

			int points = _library.TryGetPack(packId, out var pack)
				? CollectionRules.CalculatePoints(pack, progress.Caught, progress.CompletedUnixSeconds.HasValue)
				: 0;

			collection.Packs.Remove(packId);
			collection.IsDirty = true;

			if (points > 0)
				_world.Create(new UpdateScoreReq { Kind = ScoreUpdateRequestKind.Penalty, Points = points });
			else
				_world.Create(new OnGameSavingRequestedEvt());
		}
	}
}
