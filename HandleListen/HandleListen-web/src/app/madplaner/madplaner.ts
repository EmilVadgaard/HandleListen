import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { Opskrifter } from '../opskrifter/opskrifter';
import { MadplanerListe } from '../madplaner-liste/madplaner-liste';

@Component({
  selector: 'app-madplaner',
  imports: [MatTabsModule, MatIconModule, Opskrifter, MadplanerListe],
  templateUrl: './madplaner.html',
  styleUrl: './madplaner.css',
})
export class Madplaner {
}
