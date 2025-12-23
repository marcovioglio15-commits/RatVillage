using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Emergence
{
    /// <summary>
    /// Helper methods for the society clock system.
    /// </summary>
    public partial struct EmergenceSocietyClockSystem
    {
        #region Helpers
        private static int GetWindow(float timeOfDay, EmergenceSocietySchedule schedule)
        {
            if (IsInWindow(timeOfDay, schedule.SleepStartHour, schedule.SleepEndHour))
                return WindowSleep;

            if (IsInWindow(timeOfDay, schedule.WorkStartHour, schedule.WorkEndHour))
                return WindowWork;

            return WindowLeisure;
        }

        private static bool IsInWindow(float timeOfDay, float startHour, float endHour)
        {
            if (startHour == endHour)
                return false;

            if (startHour < endHour)
                return timeOfDay >= startHour && timeOfDay < endHour;

            return timeOfDay >= startHour || timeOfDay < endHour;
        }

        private static void EmitWindowSignal(int window, EmergenceSocietySchedule schedule, DynamicBuffer<EmergenceSignalEvent> signals,
            DynamicBuffer<EmergenceScheduleSignal> scheduleSignals, Entity entity, float value)
        {
            FixedString64Bytes signalId = GetWindowSignal(window, schedule);

            if (signalId.Length == 0)
                return;

            EmergenceSignalEvent signalEvent = new EmergenceSignalEvent
            {
                SignalId = signalId,
                Value = value,
                Target = entity,
                LodTier = EmergenceLodTier.Full,
                Time = 0d
            };

            signals.Add(signalEvent);
            scheduleSignals.Add(new EmergenceScheduleSignal { SignalId = signalId, Value = value });
        }

        private static void EmitTickSignal(int window, EmergenceSocietySchedule schedule, DynamicBuffer<EmergenceSignalEvent> signals,
            DynamicBuffer<EmergenceScheduleSignal> scheduleSignals, Entity entity, float value)
        {
            FixedString64Bytes signalId = GetTickSignal(window, schedule);

            if (signalId.Length == 0)
                return;

            EmergenceSignalEvent signalEvent = new EmergenceSignalEvent
            {
                SignalId = signalId,
                Value = value,
                Target = entity,
                LodTier = EmergenceLodTier.Full,
                Time = 0d
            };

            signals.Add(signalEvent);
            scheduleSignals.Add(new EmergenceScheduleSignal { SignalId = signalId, Value = value });
        }

        private static FixedString64Bytes GetWindowSignal(int window, EmergenceSocietySchedule schedule)
        {
            if (window == WindowSleep)
                return schedule.SleepSignalId;

            if (window == WindowWork)
                return schedule.WorkSignalId;

            return schedule.LeisureSignalId;
        }

        private static FixedString64Bytes GetTickSignal(int window, EmergenceSocietySchedule schedule)
        {
            if (window == WindowSleep)
                return schedule.SleepTickSignalId;

            if (window == WindowWork)
                return schedule.WorkTickSignalId;

            return schedule.LeisureTickSignalId;
        }

        private static FixedString64Bytes GetWindowLabel(int window)
        {
            if (window == WindowSleep)
                return WindowLabelSleep;

            if (window == WindowWork)
                return WindowLabelWork;

            return WindowLabelLeisure;
        }

        private static EmergenceDebugEvent BuildScheduleDebugEvent(EmergenceDebugEventType eventType, float timeOfDay, Entity society, int window, float value)
        {
            EmergenceDebugEvent debugEvent = new EmergenceDebugEvent
            {
                Type = eventType,
                Time = timeOfDay,
                Society = society,
                Subject = society,
                Target = Entity.Null,
                NeedId = default,
                ResourceId = default,
                WindowId = GetWindowLabel(window),
                Reason = default,
                Value = value
            };

            return debugEvent;
        }

        private static float GetWindowProgress(float timeOfDay, EmergenceSocietySchedule schedule, int window)
        {
            if (window == WindowSleep)
                return GetWindowProgress(timeOfDay, schedule.SleepStartHour, schedule.SleepEndHour);

            if (window == WindowWork)
                return GetWindowProgress(timeOfDay, schedule.WorkStartHour, schedule.WorkEndHour);

            return GetLeisureProgress(timeOfDay, schedule);
        }

        private static float GetWindowProgress(float timeOfDay, float startHour, float endHour)
        {
            float length = GetWindowLength(startHour, endHour);

            if (length <= 0f)
                return 0f;

            if (startHour < endHour)
                return math.saturate((timeOfDay - startHour) / length);

            if (timeOfDay >= startHour)
                return math.saturate((timeOfDay - startHour) / length);

            return math.saturate((timeOfDay + (24f - startHour)) / length);
        }

        private static float GetWindowLength(float startHour, float endHour)
        {
            if (startHour == endHour)
                return 0f;

            if (startHour < endHour)
                return endHour - startHour;

            return (24f - startHour) + endHour;
        }

        private static float GetLeisureProgress(float timeOfDay, EmergenceSocietySchedule schedule)
        {
            float segmentAStart = schedule.SleepEndHour;
            float segmentAEnd = schedule.WorkStartHour;
            float segmentBStart = schedule.WorkEndHour;
            float segmentBEnd = schedule.SleepStartHour;

            float lengthA = GetWindowLength(segmentAStart, segmentAEnd);
            float lengthB = GetWindowLength(segmentBStart, segmentBEnd);
            float totalLength = lengthA + lengthB;

            if (totalLength <= 0f)
                return 0f;

            if (IsInWindow(timeOfDay, segmentAStart, segmentAEnd))
            {
                float progressA = GetWindowProgress(timeOfDay, segmentAStart, segmentAEnd);
                float scaledA = progressA * (lengthA / totalLength);
                return math.saturate(scaledA);
            }

            if (IsInWindow(timeOfDay, segmentBStart, segmentBEnd))
            {
                float progressB = GetWindowProgress(timeOfDay, segmentBStart, segmentBEnd);
                float offset = lengthA / totalLength;
                float scaledB = offset + progressB * (lengthB / totalLength);
                return math.saturate(scaledB);
            }

            return 0f;
        }

        private static float SampleScheduleCurve(BlobAssetReference<EmergenceScheduleCurveBlob> curveReference, int window, float normalizedTime)
        {
            if (!curveReference.IsCreated)
                return 1f;

            ref EmergenceScheduleCurveBlob curve = ref curveReference.Value;

            if (curve.SampleCount <= 0)
                return 1f;

            ref BlobArray<float> samples = ref GetCurveSamples(ref curve, window);

            if (samples.Length == 0)
                return 1f;

            float scaled = math.clamp(normalizedTime, 0f, 1f) * (samples.Length - 1);
            int index = (int)math.floor(scaled);
            int nextIndex = math.min(index + 1, samples.Length - 1);
            float t = scaled - index;
            float value = math.lerp(samples[index], samples[nextIndex], t);

            return math.saturate(value);
        }

        private static ref BlobArray<float> GetCurveSamples(ref EmergenceScheduleCurveBlob curve, int window)
        {
            if (window == WindowSleep)
                return ref curve.SleepCurve;

            if (window == WindowWork)
                return ref curve.WorkCurve;

            return ref curve.LeisureCurve;
        }
        #endregion
    }
}
