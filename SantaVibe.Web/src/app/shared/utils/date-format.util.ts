import { format } from 'date-fns';
import { pl } from 'date-fns/locale';

/**
 * Format ISO date string to Polish locale format
 * Example output: "1 stycznia 2025, 10:00"
 *
 * @param isoString ISO 8601 date string from the API
 * @returns Formatted date string in Polish
 */
export function formatDateInPolish(isoString: string | undefined | null): string {
  if (!isoString) {
    return 'Brak danych';
  }

  try {
    const date = new Date(isoString);

    // Check if date is valid
    if (isNaN(date.getTime())) {
      return 'Nieprawidłowa data';
    }

    return format(date, 'd MMMM yyyy, HH:mm', { locale: pl });
  } catch {
    return 'Nieprawidłowa data';
  }
}
