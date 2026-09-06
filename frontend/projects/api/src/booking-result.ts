export interface BookingResult {
  id: string; slotId: string; startsAt: string; durationMinutes: number; mentorName: string; timeZone: string; status: 'Booked' | 'Held' | 'Cancelled'; canChange: boolean; changeReason: string | null; moduleOrdinal: number | null; moduleTitle: string | null;
}
