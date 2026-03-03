using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuctionMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "categories_parent_id_fkey",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_item_images_items_item_id",
                table: "item_images");

            migrationBuilder.DropForeignKey(
                name: "fk_item_questions_items_item_id",
                table: "item_questions");

            migrationBuilder.DropIndex(
                name: "ix_item_questions_item_id",
                table: "item_questions");

            migrationBuilder.DropIndex(
                name: "ix_item_images_item_id",
                table: "item_images");

            migrationBuilder.DropIndex(
                name: "ix_categories_parent_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "item_id1",
                table: "item_questions");

            migrationBuilder.DropColumn(
                name: "item_id1",
                table: "item_images");

            migrationBuilder.DropColumn(
                name: "parent_id1",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_item_questions_item_id",
                table: "item_questions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_images_item_id",
                table: "item_images",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "categories_parent_id_fkey",
                table: "categories",
                column: "parent_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_item_images_items_item_id",
                table: "item_images",
                column: "item_id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_item_questions_items_item_id",
                table: "item_questions",
                column: "item_id",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 255)
                .Annotation("Relational:ColumnName", "normalized_email")
                .Annotation("Relational:ComputedColumnSql", "upper((email)::text)")
                .Annotation("Relational:IsStored", true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "normalized_user_name")
                .Annotation("Relational:ComputedColumnSql", "upper((user_name)::text)")
                .Annotation("Relational:IsStored", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "categories_parent_id_fkey",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "fk_item_images_items_item_id",
                table: "item_images");

            migrationBuilder.DropForeignKey(
                name: "fk_item_questions_items_item_id",
                table: "item_questions");

            migrationBuilder.DropIndex(
                name: "ix_item_questions_item_id",
                table: "item_questions");

            migrationBuilder.DropIndex(
                name: "ix_item_images_item_id",
                table: "item_images");

            migrationBuilder.DropIndex(
                name: "ix_categories_parent_id",
                table: "categories");

            migrationBuilder.AddColumn<Guid>(
                name: "item_id1",
                table: "item_questions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "item_id1",
                table: "item_images",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "parent_id1",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_questions_item_id",
                table: "item_questions",
                column: "item_id1");

            migrationBuilder.CreateIndex(
                name: "ix_item_images_item_id",
                table: "item_images",
                column: "item_id1");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id1");

            migrationBuilder.AddForeignKey(
                name: "categories_parent_id_fkey",
                table: "categories",
                column: "parent_id1",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_item_images_items_item_id",
                table: "item_images",
                column: "item_id1",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_item_questions_items_item_id",
                table: "item_questions",
                column: "item_id1",
                principalTable: "items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 255)
                .Annotation("Relational:ColumnName", "normalized_email")
                .Annotation("Relational:ColumnType", "character varying(255)")
                .Annotation("Relational:ComputedColumnSql", "upper((email)::text)")
                .Annotation("Relational:IsStored", true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "normalized_user_name")
                .Annotation("Relational:ColumnType", "character varying(50)")
                .Annotation("Relational:ComputedColumnSql", "upper((user_name)::text)")
                .Annotation("Relational:IsStored", true);
        }
    }
}
