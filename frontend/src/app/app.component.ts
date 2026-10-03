import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="shell">
      <section class="welcome">
        <span class="eyebrow">BillingRD</span>
        <h1>Facturación simple para negocios que necesitan vender, no pelear con el sistema.</h1>
        <p>
          Base técnica inicial lista. El primer vertical slice será negocio, catálogo,
          cliente, venta, factura y pago.
        </p>
      </section>
    </main>
  `
})
export class AppComponent {}
