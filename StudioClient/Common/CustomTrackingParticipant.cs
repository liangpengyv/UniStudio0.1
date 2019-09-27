using System;
using System.Activities;
using System.Activities.Tracking;
using System.Collections.Generic;

namespace StudioClient.Common
{
    public class CustomTrackingParticipant : TrackingParticipant
    {
        public event EventHandler<TrackingEventArgs> TrackingRecordReceived;
        public Dictionary<string, Activity> ActivityIdToWorkflowElementMap { get; set; }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            OnTrackingRecordReceived(record, timeout);
        }

        // 在接收到跟踪记录时，调用 TrackingRecordReceived 以及从 TrackingParticipant 获得的记录接收信息
        // 我们也不必担心 Expressions 的跟踪数据
        protected void OnTrackingRecordReceived(TrackingRecord record, TimeSpan timeout)
        {
            System.Diagnostics.Debug.WriteLine(
                String.Format("Tracking Record Received: {0} with timeout: {1} seconds.", record, timeout.TotalSeconds)
            );

            if (TrackingRecordReceived != null)
            {
                ActivityStateRecord activityStateRecord = record as ActivityStateRecord;

                if ((activityStateRecord != null) && (!activityStateRecord.Activity.TypeName.Contains("System.Activities.Expressions")))
                {
                    if (ActivityIdToWorkflowElementMap.ContainsKey(activityStateRecord.Activity.Id))
                    {
                        TrackingRecordReceived(this, new TrackingEventArgs(
                                                        record,
                                                        timeout,
                                                        ActivityIdToWorkflowElementMap[activityStateRecord.Activity.Id]
                                                        )
                            );
                    }

                }
                else
                {
                    TrackingRecordReceived(this, new TrackingEventArgs(record, timeout, null));
                }
            }
        }
    }

    //Custom Tracking EventArgs
    public class TrackingEventArgs : EventArgs
    {
        public TrackingRecord Record { get; set; }
        public TimeSpan Timeout { get; set; }
        public Activity Activity { get; set; }

        public TrackingEventArgs(TrackingRecord trackingRecord, TimeSpan timeout, Activity activity)
        {
            this.Record = trackingRecord;
            this.Timeout = timeout;
            this.Activity = activity;
        }
    }
}
