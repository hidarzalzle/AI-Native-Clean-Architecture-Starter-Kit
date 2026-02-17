using System.Diagnostics;

namespace SharedKernel.Common;

public static class AppDiagnostics
{
    public const string ActivitySourceName = "support-triage.activity";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
