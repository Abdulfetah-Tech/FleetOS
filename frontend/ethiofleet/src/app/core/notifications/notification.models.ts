export enum NotificationType {
  VehicleDocumentExpiry = 1,
  MaintenanceDue = 2,
}

export enum NotificationSeverity {
  Information = 1,
  Warning = 2,
  Critical = 3,
}

export interface FleetNotification {
  id: string;
  organizationId: string;
  type: NotificationType;
  severity: NotificationSeverity;
  title: string;
  message: string;
  createdAt: string;
  readAt: string | null;
  isRead: boolean;
}

export interface NotificationPage {
  items: FleetNotification[];
  page: number;
  pageSize: number;
  totalCount: number;
  unreadCount: number;
}
