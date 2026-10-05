import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { CalendarSharingService } from '../calendar-sharing.service';
import { CalendarMember, CalendarInvite } from '../calendar-invite';

@Component({
  selector: 'app-share-calendar-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './share-calendar-dialog.html',
  styleUrl: './share-calendar-dialog.css',
})
export class ShareCalendarDialog {
  private dialogRef = inject(MatDialogRef<ShareCalendarDialog, boolean>);
  private sharingService = inject(CalendarSharingService);

  changed = false;

  members = signal<CalendarMember[]>([]);
  incomingInvites = signal<CalendarInvite[]>([]);
  outgoingInvites = signal<CalendarInvite[]>([]);

  inviteEmail = '';
  inviteError = signal<string | null>(null);

  constructor() {
    this.refresh();
  }

  private refresh() {
    this.sharingService.getMembers().subscribe(members => this.members.set(members));
    this.sharingService.getInvites().subscribe(invites => {
      this.incomingInvites.set(invites.filter(i => i.isIncoming));
      this.outgoingInvites.set(invites.filter(i => !i.isIncoming));
    });
  }

  sendInvite() {
    const email = this.inviteEmail.trim();
    if (!email) return;
    this.inviteError.set(null);
    this.sharingService.createInvite(email).subscribe({
      next: invite => {
        this.outgoingInvites.update(list => [...list, invite]);
        this.inviteEmail = '';
      },
      error: err => {
        this.inviteError.set(typeof err.error === 'string' ? err.error : 'Kunne ikke sende invitationen.');
      }
    });
  }

  accept(invite: CalendarInvite) {
    this.sharingService.acceptInvite(invite.id).subscribe(() => {
      this.incomingInvites.update(list => list.filter(i => i.id !== invite.id));
      this.changed = true;
      this.refresh();
    });
  }

  decline(invite: CalendarInvite) {
    this.sharingService.declineInvite(invite.id).subscribe(() => {
      this.incomingInvites.update(list => list.filter(i => i.id !== invite.id));
    });
  }

  cancel(invite: CalendarInvite) {
    this.sharingService.cancelInvite(invite.id).subscribe(() => {
      this.outgoingInvites.update(list => list.filter(i => i.id !== invite.id));
    });
  }

  leaveCalendar() {
    if (!confirm('Du forlader den delte kalender og får din egen kopi af den, som den ser ud nu. De andre beholder deres kalender uændret. Fortsæt?')) return;
    this.sharingService.leave().subscribe(() => {
      this.changed = true;
      this.close();
    });
  }

  close() {
    this.dialogRef.close(this.changed);
  }
}
