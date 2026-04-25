import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-contact',
  template: `
    <section class="flex flex-col items-center gap-6 px-4 py-16">
      <h1 class="text-3xl font-bold">Contato</h1>
      <p class="text-gray-600">Entre em contato conosco</p>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactComponent {}
