using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurnitureOrders.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "CustomerNumberSeq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "CustomerNumberSeq");
        }
    }
}
