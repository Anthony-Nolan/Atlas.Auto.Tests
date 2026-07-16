using NUnit.Framework;

// Tests are I/O-bound (waiting for API responses), not CPU-bound, so we can safely exceed the core count.
[assembly: LevelOfParallelism(50)]
