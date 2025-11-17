---
trigger: model_decision
---

# UI Rules — C# with Blazorise and .NET MAUI (Not Applicable)

SMBLibrary ships protocol libraries and a WinForms-based server host. It does not use Blazor or .NET MAUI.
Treat the guidance below as generic UI advice only if you later add such frontends; otherwise it can be ignored
for this repository.

A concise, practical ruleset for building accessible, performant UIs in **Blazor (with Blazorise)** and **.NET MAUI**. Optimized for a solo principal engineer shipping production apps.

---

## 0) North Stars

- Clarity first: Prefer predictable layouts, explicit state, and small components/views.
- MVVM everywhere: Presentation logic in ViewModels; UI is declarative and thin.
- Fast by default: Virtualize large lists, minimize re-renders, and avoid unnecessary state updates.
- Accessible: Semantics and keyboard flows are non-negotiable.
- Design system: One theme, one source of truth (variables/tokens).

---

## 1) Architecture & State

### 1.1 MVVM (MAUI & Blazor components)

- Keep ViewModels platform-agnostic; only Views touch UI APIs.
- Inject services into VMs (DI) and expose bindable properties and commands.
- In Blazor, components act as “views”; keep logic in a partial class or separate service/VM.

Do

- One responsibility per component/screen.
- Explicit input/output: parameters for child components; events/callbacks up.
- Use immutable records/DTOs for UI props when possible.

Don’t

- Hide business rules in the code-behind or Razor markup.
- Bind directly to mutable global state.

### 1.2 Navigation

- MAUI: Prefer Shell with URI routes; keep navigation in a NavigationService wrapping Shell to decouple pages.
- Blazor: Use the Router and NavigationManager; encapsulate route building in a helper/service.

### 1.3 State management

- Scope state to the smallest component possible.
- For cross-page state, use scoped services or a store; persist only what’s necessary.
- Avoid cascading parameters for “everything”; use them for theme/user/session only.

---

## 2) Blazorise Guidance

### 2.1 Component library setup

- Choose a provider (Bootstrap/Bulma/Material/Ant/Fluent) and lock theme tokens early.
- Keep a /theme/ folder with variables (colors, spacing, radii) and a design-tokens file.

### 2.2 DataGrid

- Use Virtualize for large datasets; page server-side when possible.
- Keep row objects lean; avoid heavy child components per cell.
- Defer expensive cell templating (conditional rendering) and prefer lightweight templates.
- Enable built-in sorting, filtering, and paging rather than DIY logic unless truly custom.
- Validate edits with Blazorise Validation components or grid-level validators.
- Cap visible columns; collapse advanced fields behind a details row or drawer.

### 2.3 Forms & Validation

- Centralize validation rules (FluentValidation or data annotations) and plug into Blazorise Validation.
- Show error messages inline; prevent submit until valid; disable submit while pending.

### 2.4 Feedback & Navigation UI

- Use Snackbar/Alert/Modal with strict durations and a11y focus management.
- Keep bars/menus responsive; avoid more than two levels of nesting.

---

## 3) Performance (Blazor)

- Prefer parameter changes over cascading global state to avoid broad re-renders.
- Leverage ShouldRender when a component is stable and only certain params trigger updates.
- Virtualize long lists/tables; fetch just-in-time data based on viewport (or server paging).
- Avoid StateHasChanged in tight loops; batch updates or use timers with care.
- In WebAssembly apps consider AOT where CPU-bound UI or parsing exists; weigh download size vs runtime speed.
- Use OnParametersSet[Async] carefully; avoid heavy work in lifecycle methods—push to async services.
- Prefer RenderFragment for small templating; minimize deep component trees for cells in large grids.

Diagnostics

- Enable tracing/metrics in development; profile rendering hot paths; audit large component re-renders.

---

## 4) .NET MAUI Guidance

### 4.1 Shell & Navigation

- Define routes in AppShell; prefer URI-based navigation for deep links.
- Keep page constructors minimal; page receives a ViewModel via DI.
- Centralize navigation via an INavigationService (GoToAsync, query parameters, back stack).

### 4.2 Layouts & Controls

- Use Grid for complex responsive layouts, VerticalStackLayout/HorizontalStackLayout for simple flows.
- Avoid AbsoluteLayout except for overlays. Prefer stars (\*) and Auto sizing over hard pixels.
- Virtualize or paginate long lists with CollectionView and ItemTemplate reuse.

### 4.3 MVVM specifics

- Use INotifyPropertyChanged or ObservableObject. Keep commands async where side effects occur.
- Never block the UI thread; use MainThread.BeginInvokeOnMainThread for UI updates from background work.

### 4.4 Resources & Theming

- Centralize colors, styles, and dimensions in Resources. Use DynamicResource for runtime theme changes.
- Keep font sizes and touch targets platform-appropriate; ensure contrast meets WCAG.

### 4.5 Accessibility

- Set SemanticProperties.Description and Hint on interactive elements.
- Ensure tab order and focus are logical; all actions keyboard accessible.
- Use AutomationProperties.IsInAccessibleTree appropriately; test with screen readers on each platform.

### 4.6 Platform specifics

- Prefer Handlers over renderers for custom controls.
- Use OnPlatform / OnIdiom for platform/idiom differences sparingly; default to shared UI.

---

## 5) Localization & Globalization

- Centralize strings in resources; do not hard-code UI text.
- For numbers/dates/currencies, format with the current culture; keep parsing strict and culture-aware.
- Provide language switch hooks that refresh UI (Blazor: re-render; MAUI: resource provider update + page refresh).

---

## 6) Testing

### 6.1 Blazor

- Use bUnit to render components, set parameters, and assert DOM output and events.
- Write focused tests per component; mock services via DI.
- Snapshot tests only for stable markup; otherwise assert semantics (text, attributes, roles).

### 6.2 MAUI

- Unit test ViewModels (pure logic) extensively.
- For UI flows, prefer light end-to-end smoke checks (Appium/MAUI UITest) for critical paths only.

---

## 7) Accessibility Rules (Both)

- Every interactive element must be reachable by keyboard and have a visible focus style.
- Provide names/labels (aria-label/aria-labelledby in Blazor; SemanticProperties in MAUI).
- Respect reduced-motion and color contrast; avoid text in images as the only information.
- Validate with screen readers and automated checks (axe, Accessibility Insights).

---

## 8) Error & Loading States

- All networked views implement three states: loading, content, error.
- Loading: skeletons/spinners with aria roles; set timeouts for slow ops.
- Error: concise message, recovery action, and a diagnostic code.
- Never leave indeterminate states; cancel previous requests on navigation.

---

## 9) Design System & Theming

- Lock down spacing scale (4 or 8-point), typography ramp, radii, and elevation.
- Keep token naming consistent across Blazorise variables and MAUI Resources.
- Document reusable components: primary button, input field, card, panel, banner, modal.

---

## 10) File/Folder Conventions

````text
/src
  /App
    /Theme          # design tokens, styles
    /Components     # Blazor components (small, pure)
    /Pages          # Blazor pages (route endpoints)
    /Services       # UI services (nav, dialogs, toasts)
  /Maui
    /Pages          # XAML or C# UI
    /ViewModels
    /Resources      # styles, colors, fonts
/tests
  /App.Components   # bUnit
  /Maui.ViewModels  # xUnit/NUnit
```text
---

## 11) Checklists

### Build a new screen/component

- [ ] Sketch states (empty/loading/error/content).
- [ ] Define inputs/outputs; no hidden dependencies.
- [ ] Wire to a ViewModel; no business rules in markup.
- [ ] Add a11y names/roles; keyboard flow validated.
- [ ] Add unit tests (bUnit/ViewModel).

### DataGrid (Blazorise)

- [ ] Virtualize or paginate; page size tuned.
- [ ] Server paging for very large sets.
- [ ] Column count trimmed; expensive cells templated lazily.
- [ ] Inline validation and optimistic UI where safe.
- [ ] Stable keys for rows; no random keys per render.

### MAUI page

- [ ] Shell route added; deep link tested.
- [ ] Layout uses Grid/Stack; no absolute sizes unless necessary.
- [ ] CollectionView virtualization verified.
- [ ] Semantics set; contrast and focus tested.

---

## 12) Snippets

### Blazorise DataGrid (virtualized)

```razor
<DataGrid TItem="OrderRow"
          Data="@Orders"
          Virtualize="true"
          PageSize="50"
          Responsive="true"
          RowKey="@((row) => row.Id)">
    <DataGridColumns>
        <DataGridColumn Field="@nameof(OrderRow.OrderNo)" Caption="Order" />
        <DataGridColumn Field="@nameof(OrderRow.Customer)" Caption="Customer" />
        <DataGridColumn Field="@nameof(OrderRow.Total)" Caption="Total"
                        TextAlignment="TextAlignment.Right" />
    </DataGridColumns>
</DataGrid>
```text
### MAUI Shell route + navigation service

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(nameof(OrderDetailPage), typeof(OrderDetailPage));

// NavigationService.cs
public class NavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? query = null) =>
        Shell.Current.GoToAsync(route, query);
}

// Usage
await _nav.GoToAsync($"{nameof(OrderDetailPage)}",
    new Dictionary<string, object> { ["id"] = orderId });
```text
### MAUI accessibility semantics

```xml
<Button Text="Pay"
        SemanticProperties.Description="Pay invoice"
        SemanticProperties.Hint="Submits payment for the selected invoice" />
```text
---

## 13) Defaults & Policies

- Prefer responsive layouts and virtualization; avoid client-side heavy grids without need.
- No blocking calls on UI threads. All I/O is async.
- All interactive components must have keyboard shortcuts or focusable equivalents.
- PRs/commits must include a note on performance or a11y impacts for significant UI changes.

---

## 14) When to go custom

- Write custom components only when the design system needs cannot be met by Blazorise/MAUI built-ins.
- Encapsulate platform differences behind handlers (MAUI) and thin adapters (Blazor).

---

## 15) References (keep locally in docs/links.md)

- Blazor: performance/virtualization, metrics, localization.
- Blazorise: DataGrid, Validation.
- MAUI: Shell navigation, MVVM, Accessibility, Layouts.

---

Adopt these rules incrementally. Favor smaller, faster components and pages with explicit state and predictable navigation. Ship, profile, and tighten the loop.

````
