using System;
using System.Globalization;

namespace AutoTerrainDesignations.Access
{
    internal static class AccesswayProgressPresentation
    {
        internal static bool ShouldShowToast(ATDAccesswayHandleSnapshot snapshot,
            double activeWallSeconds)
            // Polling a background encoder/worker barely consumes game-thread
            // processing time. Visibility follows how long the player has waited.
            => !snapshot.IsTerminal && activeWallSeconds >= 0.25d;

        private const string ReplayRecordingPrefix = "Recording access replay";
        private const string ReplayCancellingPrefix = "Cancelling access replay";

        internal static bool IsReplayCapture(ATDAccesswayHandleSnapshot snapshot)
            => snapshot.Phase == "Starting replay capture"
                || snapshot.Phase.StartsWith(ReplayRecordingPrefix, StringComparison.Ordinal)
                || snapshot.Phase.StartsWith(ReplayCancellingPrefix, StringComparison.Ordinal);

        internal static string FormatPhase(ATDAccesswayHandleSnapshot snapshot)
        {
            string phase = snapshot.Phase;
            if (phase.StartsWith(ReplayCancellingPrefix, StringComparison.Ordinal))
                return AtdLocalization.AccessPhaseCancelling.TranslatedString;
            if (IsReplayCapture(snapshot))
            {
                // The recorder still reports its detailed stage in logs. Its UI shows the final (NN%) suffix.
                int percentStart = phase.LastIndexOf(" (", StringComparison.Ordinal);
                if (percentStart >= 0 && phase.EndsWith("%)", StringComparison.Ordinal))
                {
                    string percent = phase.Substring(percentStart + 2, phase.Length - percentStart - 3);
                    return string.Format(AtdLocalization.AccessPhaseRecordingPercent.TranslatedString, percent);
                }
                return AtdLocalization.AccessPhaseRecording.TranslatedString;
            }

            return (phase switch
            {
                "Capturing terrain" => AtdLocalization.AccessPhaseCapturingTerrain,
                "Capturing mine terrain" => AtdLocalization.AccessPhaseCapturingMine,
                "Capturing mine safety footprint" => AtdLocalization.AccessPhaseCapturingSafety,
                "Planning mine" => AtdLocalization.AccessPhasePlanningMine,
                "Projecting designations" => AtdLocalization.AccessPhaseProjecting,
                "Building navigation" => AtdLocalization.AccessPhaseBuildingNavigation,
                "Finalizing snapshot" => AtdLocalization.AccessPhaseFinalizingSnapshot,
                "Building V1 ground routes" => AtdLocalization.AccessPhaseGroundRoutesV1,
                "Building V1 goal potential" => AtdLocalization.AccessPhaseGoalPotentialV1,
                "Indexing occupied terrain" => AtdLocalization.AccessPhaseIndexingOccupied,
                "Projecting fixed V2 ground" => AtdLocalization.AccessPhaseProjectingFixedV2,
                "Building V2 fixed navigation" => AtdLocalization.AccessPhaseFixedNavigationV2,
                "Constructing snapshot" => AtdLocalization.AccessPhaseConstructingSnapshot,
                "Waiting for previous worker job to stop" => AtdLocalization.AccessPhasePreviousWorker,
                "Preparing search request" => AtdLocalization.AccessPhasePreparingRequest,
                "Preparing search session" => AtdLocalization.AccessPhasePreparingSession,
                "Preparing search workspace" => AtdLocalization.AccessPhasePreparingWorkspace,
                "Evaluating legacy access candidates" => AtdLocalization.AccessPhaseEvaluatingLegacy,
                "Searching" => AtdLocalization.AccessPhaseSearching,
                "Collecting search goals" => AtdLocalization.AccessPhaseCollectingGoals,
                "Finalizing search session" => AtdLocalization.AccessPhaseFinalizingSession,
                "Search session ready" => AtdLocalization.AccessPhaseSessionReady,
                "Building goal distance index" => AtdLocalization.AccessPhaseBuildingGoalIndex,
                "Classifying V2 ground cleanup" => AtdLocalization.AccessPhaseClassifyingCleanupV2,
                "Building V2 ground components" => AtdLocalization.AccessPhaseGroundComponentsV2,
                "Building V2 ground potential" => AtdLocalization.AccessPhaseGroundPotentialV2,
                "V2 frontier" => AtdLocalization.AccessPhaseFrontierV2,
                "Ground suffix" => AtdLocalization.AccessPhaseGroundSuffix,
                "Handoff entries" => AtdLocalization.AccessPhaseHandoffs,
                _ => AtdLocalization.AccessPhasePreparing
            }).TranslatedString;
        }

        internal static string FormatStats(
            ATDAccesswayHandleSnapshot snapshot,
            int maxVisitedNodes,
            int sliceBudgetMilliseconds,
            int timeoutSeconds)
        {
            if (IsReplayCapture(snapshot))
                return AtdLocalization.AccessProgressReplayStats.TranslatedString;
            if (snapshot.ExecutionBackend == ATDAccesswayExecutionBackend.Worker)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    AtdLocalization.AccessProgressWorkerStats.TranslatedString,
                    snapshot.VisitedNodes, maxVisitedNodes, snapshot.PendingNodes,
                    snapshot.StatusElapsedMilliseconds / 1000d, timeoutSeconds);
            }
            return string.Format(CultureInfo.InvariantCulture,
                AtdLocalization.AccessProgressFrameStats.TranslatedString,
                snapshot.VisitedNodes, maxVisitedNodes, snapshot.PendingNodes,
                sliceBudgetMilliseconds, snapshot.ProcessingMilliseconds / 1000d, timeoutSeconds);
        }
    }
}
