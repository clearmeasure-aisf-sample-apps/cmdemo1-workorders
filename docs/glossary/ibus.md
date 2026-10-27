# IBus

**IBus** is the application abstraction in this application's CQRS flow. [`IBus`](../../src/Core/IBus.cs) in `src/Core` declares `Send` and `Publish`; [`Bus`](../../src/UI.Shared/Bus.cs) in `src/UI.Shared` implements them by forwarding each call to MediatR. The path is described in [CQRS request flow](../conventions/cqrs-request-flow.md).
