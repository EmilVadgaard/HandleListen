export interface RecipeBookMember {
    userId: string;
    email: string;
}

export interface RecipeBookInvite {
    id: number;
    fromEmail: string;
    toEmail: string;
    isIncoming: boolean;
    createdAt: string;
}
