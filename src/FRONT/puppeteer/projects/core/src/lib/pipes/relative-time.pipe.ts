import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'relativeTime' })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';

    const date = new Date(value);
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

    if (seconds < 60) return 'agora';

    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return minutes === 1 ? 'há 1 minuto' : `há ${minutes} minutos`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours === 1 ? 'há 1 hora' : `há ${hours} horas`;

    const days = Math.floor(hours / 24);
    return days === 1 ? 'há 1 dia' : `há ${days} dias`;
  }
}
