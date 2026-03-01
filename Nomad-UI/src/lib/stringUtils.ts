/**
 * Converts a string to Title Case (e.g., "JOHN DOE" -> "John Doe")
 */
export function toTitleCase(name: string | null | undefined): string {
    if (!name) return '';

    return name
        .toLowerCase()
        .split(' ')
        .filter(word => word.length > 0)
        .map(word => word.charAt(0).toUpperCase() + word.slice(1))
        .join(' ');
}
