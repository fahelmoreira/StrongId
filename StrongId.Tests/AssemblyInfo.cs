using Xunit;

// Several tests mutate the process-wide StrongIdDefaults.Options (IgnoreSuffixValidation,
// UseSalt). Running test classes in parallel lets those mutations leak into unrelated
// validation tests, so run them serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
