using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace ActivitySourceConsole
{
    internal class Program
    {
        private readonly static ActivitySource activitySource = new("MyCompany.MyProduct.MyLibrary", "1.0.0");

        static void Main()
        {
            _ = Sdk.CreateTracerProviderBuilder()
                .AddSource("MyCompany.MyProduct.MyLibrary")
                 .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug | ConsoleExporterOutputTargets.Console)
                .Build();

            var activity = activitySource.StartActivity("ActivityName");

            activity?.SetTag("http.method", "GET");

            // IsAllDataRequested is the same as Span.IsRecording and will be
            // false when samplers decide to not record the activity, and this
            // can be used to avoid any expensive operation to retrieve tags.
            if (activity != null && activity.IsAllDataRequested == true)
            {
                activity.SetTag("http.url", "http://www.mywebsite.com");
            }

            activity?.Stop();

            // Activity has a property called ActivityKind which represents
            // OpenTelemetry SpanKind. The default value will be Internal.
            // StartActivity allows passing the ActivityKind while starting an
            // Activity.
            activitySource.StartActivity("ActivityNameWithKind", ActivityKind.Server)?.Stop();

            // ActivityContext represents the OpenTelemetry SpanContext.
            // While starting a new Activity, the currently active Activity
            // is automatically taken as the parent of the new activity being
            // created. StartActivity allows passing explicit ActivityContext
            // to override this behavior.
            var parentContext = new ActivityContext
            (ActivityTraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c")
            , ActivitySpanId.CreateFromString("b7ad6b7169203331")
            , ActivityTraceFlags.None
            );

            // FIXME: childActivity is null
            var childActivity = activitySource.StartActivity("ActivityNameWithParentContext", ActivityKind.Server, parentContext);
            childActivity?.Stop();

            // As ActivityContext follows the W3C Trace-Context, it is also
            // possible to provide the parent context as a single string matching
            // the traceparent header of the W3C Trace-Context. This is shown below.
            activitySource.StartActivity(
                "W3ActivityName",
                ActivityKind.Server,
                "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")?.Stop();

            using (var activity2 = activitySource.StartActivity("ActivityNameWithUsing"))
            {
                activity2?.SetTag("http.method", "GET");
            } // Activity gets stopped automatically at end of this block during dispose.

            // Tags in Activity represents the OpenTelemetry Span Attributes.
            // Earlier sample showed the usage of SetTag method of Activity to
            // add tags.

            // It is also possible to provide an initial set of tags during
            // activity creation, as shown below. It is recommended to provide
            // all available Tags during activity creation itself, as Samplers
            // can only consider information present during activity creation time.
#pragma warning disable IDE0028 // Simplify collection initialization
            var initialTags = new ActivityTagsCollection();
#pragma warning restore IDE0028 // Simplify collection initialization

            initialTags["com.mycompany.product.mytag1"] = "tagValue1";
            initialTags["com.mycompany.product.mytag2"] = "tagValue2";

            activitySource.StartActivity(
                "ActivityNameWithInitialTags",
                ActivityKind.Server,
                "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
                initialTags)?.Stop();

            // In addition to parent-child relationships, activities can also be
            // linked using ActivityLinks, which represent Links in OpenTelemetry.
            // Providing activity links during creation is recommended, as this
            // allows samplers to consider them when deciding whether to sample
            // an activity. However, starting with System.Diagnostics.DiagnosticSource 9.0.0,
            // links can also be added after an activity is created.

            var activityLinks = new List<ActivityLink>();

            var linkedContext1 = new ActivityContext(
                ActivityTraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c"),
                ActivitySpanId.CreateFromString("b7ad6b7169203331"),
                ActivityTraceFlags.None);

            var linkedContext2 = new ActivityContext(
                ActivityTraceId.CreateFromString("4bf92f3577b34da6a3ce929d0e0e4736"),
                ActivitySpanId.CreateFromString("00f067aa0ba902b7"),
                ActivityTraceFlags.Recorded);

            activityLinks.Add(new ActivityLink(linkedContext1));
            activityLinks.Add(new ActivityLink(linkedContext2));

            var linkActivity = activitySource.StartActivity(
                "ActivityWithLinks",
                ActivityKind.Server,
                default(ActivityContext),
                initialTags,
                activityLinks); // links provided at creation time.

            // One may add links after activity is created too.
            var linkedContext3 = new ActivityContext(
                ActivityTraceId.CreateFromString("01260a70a81e1fa3ad5a8acfeaa0f711"),
                ActivitySpanId.CreateFromString("34739aa9e2239da1"),
                ActivityTraceFlags.None);
            linkActivity?.AddLink(new ActivityLink(linkedContext3));
            linkActivity?.Stop();

            var eventActivity = activitySource.StartActivity("EventActivityName", ActivityKind.Internal);

            eventActivity?.AddEvent(new ActivityEvent("sample activity event."));
            eventActivity?.SetStatus(ActivityStatusCode.Ok);
            eventActivity?.SetStatus(ActivityStatusCode.Error, "Error Description");

            // Prior to DiagnosticSource 6.0 there was no Status field in Activity,
            // and hence Status is set to an Activity using the following special tags:
            // otel.status_code is the Tag name used to store the StatusCode,
            // and otel.status_description is the Tag name used to store the
            // optional Description.

            // Values for the StatusCode tag must be one of the strings "UNSET",
            // "OK", or "ERROR", which correspond respectively to the enums Unset,
            // Ok, and Error from StatusCode.

            eventActivity?.SetTag("otel.status_code", "ERROR");
            eventActivity?.SetTag("otel.status_description", "error status description");

            eventActivity?.Stop();

            System.Console.WriteLine("Press Enter key to exit.");
            System.Console.ReadLine();
        }
    }
}
