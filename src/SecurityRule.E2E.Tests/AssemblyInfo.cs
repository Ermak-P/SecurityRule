// Run each feature (NUnit TestFixture) in parallel.
// Each feature gets its own TestWebServer, Playwright browser, and isolated in-memory database
// via the [BeforeFeature] / [AfterFeature] hooks in Hooks.cs.
// Scenarios within a single feature still run sequentially and share one server.
[assembly: NUnit.Framework.Parallelizable(NUnit.Framework.ParallelScope.Fixtures)]
