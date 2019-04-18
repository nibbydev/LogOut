using Utility;

namespace Test {
    public static class Program {
        public static void Main(string[] args) {
            var tracker = new HealthTracker();
            var processor = new HealthProcessor(tracker);

            tracker.updateTrackerCaptureLocation = processor.UpdateCaptureLocation;

            //tracker.OverWriteGameHandle("notepad++");
            tracker.FindGameHandle();
            
            processor.Run();
        }
    }
}