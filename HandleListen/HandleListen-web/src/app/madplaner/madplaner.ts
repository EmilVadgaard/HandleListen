import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { Opskrifter } from '../opskrifter/opskrifter';

@Component({
  selector: 'app-madplaner',
  imports: [MatTabsModule, MatIconModule, Opskrifter],
  templateUrl: './madplaner.html',
  styleUrl: './madplaner.css',
})
export class Madplaner {
}
