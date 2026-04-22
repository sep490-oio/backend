using System.Runtime.CompilerServices;

// Expose internals to the application test project for direct-handler testing.
// The ordering test for TermsDocumentActivatedEventHandler (plan §3.6.2 / B5) needs to
// instantiate the handler and pin the cache→SignalR call sequence with sequence-recording fakes.
[assembly: InternalsVisibleTo("OIO.Application.Tests")]
