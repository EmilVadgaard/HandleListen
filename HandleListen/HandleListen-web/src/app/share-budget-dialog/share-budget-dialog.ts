import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { BudgetSharingService } from '../budget-sharing.service';
import { BudgetMember, BudgetInvite } from '../budget';

@Component({
  selector: 'app-share-budget-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './share-budget-dialog.html',
  styleUrl: './share-budget-dialog.css',
})
export class ShareBudgetDialog {
  private dialogRef = inject(MatDialogRef<ShareBudgetDialog, boolean>);
  private sharingService = inject(BudgetSharingService);

  changed = false;

  members = signal<BudgetMember[]>([]);
  incomingInvites = signal<BudgetInvite[]>([]);
  outgoingInvites = signal<BudgetInvite[]>([]);

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

  accept(invite: BudgetInvite) {
    this.sharingService.acceptInvite(invite.id).subscribe(() => {
      this.incomingInvites.update(list => list.filter(i => i.id !== invite.id));
      this.changed = true;
      this.refresh();
    });
  }

  decline(invite: BudgetInvite) {
    this.sharingService.declineInvite(invite.id).subscribe(() => {
      this.incomingInvites.update(list => list.filter(i => i.id !== invite.id));
    });
  }

  cancel(invite: BudgetInvite) {
    this.sharingService.cancelInvite(invite.id).subscribe(() => {
      this.outgoingInvites.update(list => list.filter(i => i.id !== invite.id));
    });
  }

  leaveBudget() {
    if (!confirm('Du forlader det delte budget og får din egen kopi af det, som det ser ud nu. De andre beholder deres budget uændret. Fortsæt?')) return;
    this.sharingService.leave().subscribe(() => {
      this.changed = true;
      this.close();
    });
  }

  close() {
    this.dialogRef.close(this.changed);
  }
}
