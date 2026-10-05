import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
    private auth = inject(AuthService);
    private connection?: signalR.HubConnection;
    private startPromise?: Promise<void>;

    connect(): Promise<void> {
        if (!this.connection) {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/shopping-list', {
                    accessTokenFactory: () => this.auth.token() ?? ''
                })
                .withAutomaticReconnect()
                .build();
        }

        if (!this.startPromise) {
            this.startPromise = this.connection.start().catch(err => {
                console.error('SignalR connection failed', err);
                this.startPromise = undefined;
            });
        }

        return this.startPromise;
    }

    onReconnected(callback: () => void) {
        this.connect();
        this.connection?.onreconnected(() => callback());
    }

    async joinList(listId: number) {
        await this.connect();
        await this.connection?.invoke('JoinList', listId).catch(err => console.error('Failed to join list group', err));
    }

    async leaveList(listId: number) {
        await this.connect();
        await this.connection?.invoke('LeaveList', listId).catch(err => console.error('Failed to leave list group', err));
    }

    onItemsChanged(callback: (listId: number) => void) {
        this.connect().then(() => this.connection?.on('ItemsChanged', callback));
    }

    onListsChanged(callback: () => void) {
        this.connect().then(() => this.connection?.on('ListsChanged', callback));
    }

    async joinCalendar(calendarId: number) {
        await this.connect();
        await this.connection?.invoke('JoinCalendar', calendarId).catch(err => console.error('Failed to join calendar group', err));
    }

    async leaveCalendar(calendarId: number) {
        await this.connect();
        await this.connection?.invoke('LeaveCalendar', calendarId).catch(err => console.error('Failed to leave calendar group', err));
    }

    onCalendarEventsChanged(callback: (calendarId: number) => void) {
        this.connect().then(() => this.connection?.on('CalendarEventsChanged', callback));
    }

    onCalendarChanged(callback: () => void) {
        this.connect().then(() => this.connection?.on('CalendarChanged', callback));
    }

    async joinRecipe(recipeId: number) {
        await this.connect();
        await this.connection?.invoke('JoinRecipe', recipeId).catch(err => console.error('Failed to join recipe group', err));
    }

    async leaveRecipe(recipeId: number) {
        await this.connect();
        await this.connection?.invoke('LeaveRecipe', recipeId).catch(err => console.error('Failed to leave recipe group', err));
    }

    onRecipeIngredientsChanged(callback: (recipeId: number) => void) {
        this.connect().then(() => this.connection?.on('RecipeIngredientsChanged', callback));
    }

    onRecipesChanged(callback: () => void) {
        this.connect().then(() => this.connection?.on('RecipesChanged', callback));
    }

    onMealPlansChanged(callback: () => void) {
        this.connect().then(() => this.connection?.on('MealPlansChanged', callback));
    }
}
