import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { DayOption } from '../day-option';
import { SlotOption } from '../slot-option';
@Component({
  selector: 'qbs-slot-picker', imports: [], templateUrl: './slot-picker.component.html', styleUrl: './slot-picker.component.css'
})
export class SlotPickerComponent {
  readonly days = input.required<readonly DayOption[]>(); readonly slots = input.required<readonly SlotOption[]>();
  readonly selectedDay = input.required<string>(); readonly selectedSlot = input(''); readonly disabled = input(false); readonly timeZone = input.required<string>();
  readonly dayChange = output<string>(); readonly slotChange = output<string>();
}
