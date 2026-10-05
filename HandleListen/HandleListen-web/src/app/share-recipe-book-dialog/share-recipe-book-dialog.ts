import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { RecipeBookSharingService } from '../recipe-book-sharing.service';
import { RecipeBookMember, RecipeBookInvite } from '../recipe-book-invite';

@Component({
  selector: 'app-share-recipe-book-dialog',
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatListModule, MatDividerModule],
  templateUrl: './share-recipe-book-dialog.html',
  styleUrl: './share-recipe-book-dialog.css',
})
export class ShareRecipeBookDialog {
  private dialogRef = inject(MatDialogRef<ShareRecipeBookDialog, boolean>);
  private sharingService = inject(RecipeBookSharingService);

  changed = false;

  members = signal<RecipeBookMember[]>([]);
  incomingInvites = signal<RecipeBookInvite[]>([]);
  outgoingInvites = signal<RecipeBookInvite[]>([]);

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

  accept(invite: RecipeBookInvite) {
    this.sharingService.acceptInvite(invite.id).subscribe(() => {
      this.incomingInvites.update(list => list.filter(i => i.id !== invite.id));
      this.changed = true;
      this.refresh();
    });
  }

  decline(invite: RecipeBookInvite) {
    this.sharingService.declineInvite(invite.id).subscribe(() => {
      this.incomingInvites.update(list => list.filter(i => i.id !== invite.id));
    });
  }

  cancel(invite: RecipeBookInvite) {
    this.sharingService.cancelInvite(invite.id).subscribe(() => {
      this.outgoingInvites.update(list => list.filter(i => i.id !== invite.id));
    });
  }

  leaveBook() {
    if (!confirm('Du forlader den delte opskriftsbog og får din egen kopi af den, som den ser ud nu. De andre beholder deres opskriftsbog uændret. Fortsæt?')) return;
    this.sharingService.leave().subscribe(() => {
      this.changed = true;
      this.close();
    });
  }

  close() {
    this.dialogRef.close(this.changed);
  }
}
