using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TracingShimConsole
{
    internal class Program
    {
        static void Main()
        {
            // Enable OpenTelemetry for the source "MyCompany.MyProduct.MyWebServer"
            // and use a single pipeline with a custom MyProcessor, and Console exporter.
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource("MyCompany.MyProduct.MyWebServer")
                .ConfigureResource(r => r.AddService("MyServiceName"))
                .AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug | ConsoleExporterOutputTargets.Console)
                .Build();

            // The above line is required only in applications
            // which decide to use OpenTelemetry.

            var tracer = TracerProvider.Default.GetTracer("MyCompany.MyProduct.MyWebServer","1.2.3.4");
            using (var parentSpan = tracer.StartActiveSpan("parent span"))
            {
                parentSpan.SetAttribute("mystring", "value");
                parentSpan.SetAttribute("myint", 100);
                parentSpan.SetAttribute("mydouble", 101.089);
                parentSpan.SetAttribute("mybool", true);
                parentSpan.UpdateName("parent span new name");

                var childSpan = tracer.StartSpan("child span");

                IEnumerable<KeyValuePair<string, object?>> eventAttributes =
                    [new KeyValuePair<string, object?>( "key1","value1")
                    ,new KeyValuePair<string, object?>( "key2","value2")
                    ];
                childSpan.AddEvent("sample event", new SpanAttributes(eventAttributes)).SetAttribute("ch", "value").SetAttribute("more", "attributes");
                childSpan.SetStatus(Status.Ok);
                childSpan.End();
            }

            using (var exceptionSpan = tracer.StartActiveSpan("exception span"))
            {
                try
                {
                    throw new OutOfMemoryException();
                }
                catch (Exception ex)
                {
                    exceptionSpan.RecordException(ex);
                }
            }

            System.Console.WriteLine("Press Enter key to exit.");
            System.Console.ReadLine();
        }
    }
}
