// PDF Export functionality for ClassicFit Management System
// Uses browser's built-in print dialog to generate PDF

window.exportToPdf = function (title) {
    // Create a new window for printing
    var printContent = document.querySelector('.p-8') || document.querySelector('.space-y-6') || document.querySelector('main');
    
    if (!printContent) {
        alert('No content found to export.');
        return;
    }

    var printWindow = window.open('', '_blank', 'width=900,height=700');
    
    printWindow.document.write('<!DOCTYPE html><html><head><title>' + (title || 'ClassicFit Report') + '</title>');
    printWindow.document.write('<style>');
    printWindow.document.write(`
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
            color: #1a1a1a; 
            padding: 20px;
            font-size: 12px;
        }
        .print-header {
            text-align: center;
            border-bottom: 2px solid #80A1BA;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }
        .print-header h1 { font-size: 22px; color: #1E3A5F; margin-bottom: 5px; }
        .print-header p { font-size: 11px; color: #666; }
        table { width: 100%; border-collapse: collapse; margin: 10px 0; }
        th, td { border: 1px solid #ddd; padding: 6px 10px; text-align: left; font-size: 11px; }
        th { background-color: #f8f9fa; font-weight: 600; color: #374151; text-transform: uppercase; font-size: 10px; }
        tr:nth-child(even) { background-color: #f9fafb; }
        .stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin: 15px 0; }
        .stat-card { border: 1px solid #e5e7eb; border-radius: 8px; padding: 12px; text-align: center; }
        .stat-card .label { font-size: 10px; color: #6b7280; text-transform: uppercase; }
        .stat-card .value { font-size: 20px; font-weight: 700; color: #1f2937; margin-top: 4px; }
        .badge { display: inline-block; padding: 2px 8px; border-radius: 10px; font-size: 10px; font-weight: 600; }
        .badge-green { background: #dcfce7; color: #166534; }
        .badge-yellow { background: #fef9c3; color: #854d0e; }
        .badge-red { background: #fee2e2; color: #991b1b; }
        .badge-blue { background: #dbeafe; color: #1e40af; }
        .badge-purple { background: #f3e8ff; color: #6b21a8; }
        .badge-gray { background: #f3f4f6; color: #374151; }
        .footer { 
            margin-top: 30px; 
            padding-top: 10px; 
            border-top: 1px solid #e5e7eb; 
            font-size: 10px; 
            color: #9ca3af; 
            text-align: center; 
        }
        @media print {
            body { padding: 0; }
            .no-print { display: none !important; }
        }
    `);
    printWindow.document.write('</style></head><body>');
    
    // Header
    printWindow.document.write('<div class="print-header">');
    printWindow.document.write('<h1>' + (title || 'ClassicFit Report') + '</h1>');
    printWindow.document.write('<p>Generated on ' + new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' }) + '</p>');
    printWindow.document.write('</div>');
    
    // Clone and clean content
    var clone = printContent.cloneNode(true);
    
    // Remove buttons, loading spinners, search bars from clone
    var removeElements = clone.querySelectorAll('button, .animate-spin, .animate-pulse, select, input, svg');
    removeElements.forEach(function(el) {
        // Keep SVG if it's inside a badge/stat, remove interactive elements
        if (el.tagName === 'BUTTON' || el.tagName === 'SELECT' || el.tagName === 'INPUT' || el.classList.contains('animate-spin')) {
            el.remove();
        }
    });
    
    // Remove all SVG icons (they don't print well)
    var svgs = clone.querySelectorAll('svg');
    svgs.forEach(function(svg) { svg.remove(); });
    
    printWindow.document.write(clone.innerHTML);
    
    // Footer
    printWindow.document.write('<div class="footer">ClassicFit Management System &copy; ' + new Date().getFullYear() + ' | This report was generated automatically.</div>');
    
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    
    // Wait for content to load, then print
    printWindow.onload = function() {
        printWindow.focus();
        printWindow.print();
    };
    
    // Fallback for browsers that don't fire onload for dynamically written content
    setTimeout(function() {
        printWindow.focus();
        printWindow.print();
    }, 500);
};

// Simpler version that just uses window.print() on current page
window.printCurrentPage = function (title) {
    var originalTitle = document.title;
    if (title) document.title = title;
    window.print();
    document.title = originalTitle;
};
