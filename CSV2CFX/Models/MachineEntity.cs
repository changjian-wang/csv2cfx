using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV2CFX.Models
{
    public class MachineEntity
    {
        /// <summary>
        /// 设备唯一标识符
        /// </summary>
        public string? UniqueId { get; set; } = "d--0000-0000-0012-0444";

        /// <summary>
        /// 区域名称
        /// </summary>
        public string? AreaName { get; set; }

        /// <summary>
        /// 产线名称
        /// </summary>
        public string? LineName { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// 设备位置序号
        /// </summary>
        public string? Position { get; set; }

        /// <summary>
        /// 是否为SMT设备
        /// </summary>
        public string? SMTNonSMT { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public string? MachineType { get; set; }

        /// <summary>
        /// 设备品牌
        /// </summary>
        public string? Brand { get; set; }

        /// <summary>
        /// 设备型号
        /// </summary>
        public string? Mode { get; set; }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// 设备关联年份
        /// </summary>
        public string? Year { get; set; }

        /// <summary>
        /// 资产编号
        /// </summary>
        public string? AssetNumber { get; set; }

        /// <summary>
        /// 区域标识id
        /// </summary>
        public string? AreaId { get; set; }

        /// <summary>
        /// 产线标识id
        /// </summary>
        public string? LineId { get; set; }

        /// <summary>
        /// 设备标识id
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// RabbitMQ主题名称
        /// </summary>
        public string? RabbitmqTopicName { get; set; }

        /// <summary>
        /// 客户信息
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// 设备IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 操作系统类型
        /// </summary>
        public string? OperationSystem { get; set; }

        /// <summary>
        /// 流程分组
        /// </summary>
        public string? ProcessGroup { get; set; }

        /// <summary>
        /// 流程步骤
        /// </summary>
        public string? ProcessStep { get; set; }

        /// <summary>
        /// 流程代码
        /// </summary>
        public string? ProcessCode { get; set; }

        /// <summary>
        /// 是否为首个流程
        /// </summary>
        public bool? FirstProcess { get; set; }

        /// <summary>
        /// 是否为最后一个流程
        /// </summary>
        public bool? LastProcess { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string? CreateDate { get; set; }
    }
}
